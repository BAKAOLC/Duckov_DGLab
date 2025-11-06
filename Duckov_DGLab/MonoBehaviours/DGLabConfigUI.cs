using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Duckov_DGLab.Configs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Duckov_DGLab.MonoBehaviours
{
    public class DGLabConfigUI : MonoBehaviour
    {
        private MonoBehaviour? _cameraController;
        private bool _cameraLockDisabled;
        private CharacterInputControl? _charInput;
        private Button? _closeButton;
        private DGLabConfig? _config;
        private Slider? _deathDurationSlider;
        private Text? _deathDurationText;
        private Dropdown? _deathWaveTypeDropdown;
        private Slider? _defaultStrengthSlider;
        private Text? _defaultStrengthText;

        private Slider? _hurtDurationSlider;
        private Text? _hurtDurationText;
        private Dropdown? _hurtWaveTypeDropdown;
        private bool _isInitialized;
        private bool _isWaitingForKeyInput;
        private Slider? _maxStrengthSlider;
        private Text? _maxStrengthText;
        private GameObject? _overlay;
        private GameObject? _panelRoot;
        private PlayerInput? _playerInput;
        private KeyCode _toggleKey = KeyCode.F10;
        private bool _uiActive;
        private GameObject? _uiRoot;

        private void Start()
        {
            _config = ModBehaviour.Instance?.Config;
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                if (CharacterMainControl.Main != null) InitializeUI();
                return;
            }

            if (_config == null) return;

            if (IsTypingInInputField() || _panelRoot == null) return;

            if (_isWaitingForKeyInput)
            {
                HandleKeyInputCapture();
                return;
            }

            if (Input.GetKeyDown(_toggleKey))
            {
                if (_panelRoot.activeSelf)
                    HidePanel();
                else
                    ShowPanel();
            }

            if (_uiActive && !_isWaitingForKeyInput && Input.GetKeyDown(KeyCode.Escape)) HidePanel();

            if (!_uiActive) return;
            if (_charInput != null && _charInput.enabled) _charInput.enabled = false;

            if (_playerInput != null && _playerInput.inputIsActive) _playerInput.DeactivateInput();
        }

        private void LateUpdate()
        {
            if (!_uiActive || _panelRoot == null || !_panelRoot.activeSelf) return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnDestroy()
        {
            if (_charInput != null) _charInput.enabled = true;
            if (_playerInput != null) _playerInput.ActivateInput();
            if (_cameraLockDisabled && _cameraController != null) _cameraController.enabled = true;
        }

        private void InitializeUI()
        {
            if (_isInitialized) return;

            CreateOrFindUiRoot();
            BuildPanel();
            HidePanel();
            _isInitialized = true;
            ModLogger.Log("DGLabConfigUI initialized.");
        }

        private void CreateOrFindUiRoot()
        {
            var existing = GameObject.Find("DuckovDGLabCanvas");
            if (existing != null)
            {
                _uiRoot = existing;
                return;
            }

            var canvas = new GameObject("DuckovDGLabCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 9999;
            canvas.AddComponent<GraphicRaycaster>();

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _uiRoot = canvas;
            DontDestroyOnLoad(_uiRoot);
        }

        private void BuildPanel()
        {
            if (_uiRoot == null || _config == null) return;

            _overlay = new("Overlay", typeof(Image));
            _overlay.transform.SetParent(_uiRoot.transform, false);
            var overlayImage = _overlay.GetComponent<Image>();
            overlayImage.color = new(0, 0, 0, 0.5f);
            var overlayRect = _overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            _panelRoot = new("Panel", typeof(Image));
            _panelRoot.transform.SetParent(_uiRoot.transform, false);
            var panelImage = _panelRoot.GetComponent<Image>();
            panelImage.color = new(0.1f, 0.12f, 0.15f, 0.95f);
            var panelRect = _panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new(0.5f, 0.5f);
            panelRect.anchorMax = new(0.5f, 0.5f);
            panelRect.pivot = new(0.5f, 0.5f);
            panelRect.sizeDelta = new(800, 600);
            panelRect.anchoredPosition = Vector2.zero;

            var outline = _panelRoot.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(2, -2);

            BuildTitle();
            BuildContent();
            BuildCloseButton();
        }

        private void BuildTitle()
        {
            if (_panelRoot == null) return;

            var title = new GameObject("Title", typeof(Text));
            title.transform.SetParent(_panelRoot.transform, false);
            var titleText = title.GetComponent<Text>();
            titleText.text = "DGLab 配置";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new(0, 1);
            titleRect.anchorMax = new(1, 1);
            titleRect.pivot = new(0.5f, 1);
            titleRect.anchoredPosition = new(0, -20);
            titleRect.sizeDelta = new(0, 40);
        }

        private void BuildContent()
        {
            if (_panelRoot == null || _config == null) return;

            var contentArea = new GameObject("ContentArea", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentArea.transform.SetParent(_panelRoot.transform, false);
            var contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = new(0, 0);
            contentRect.anchorMax = new(1, 1);
            contentRect.offsetMin = new(20, 60);
            contentRect.offsetMax = new(-20, -60);

            var layoutGroup = contentArea.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new(10, 10, 10, 10);
            layoutGroup.spacing = 15;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            BuildHurtDurationSetting(contentArea);
            BuildHurtWaveTypeSetting(contentArea);
            BuildDeathDurationSetting(contentArea);
            BuildDeathWaveTypeSetting(contentArea);
            BuildDefaultStrengthSetting(contentArea);
            BuildMaxStrengthSetting(contentArea);
        }

        private void BuildHurtDurationSetting(GameObject parent)
        {
            if (_config == null) return;

            var settingObj = new GameObject("HurtDurationSetting", typeof(RectTransform));
            settingObj.transform.SetParent(parent.transform, false);
            var settingRect = settingObj.GetComponent<RectTransform>();
            settingRect.sizeDelta = new(0, 50);

            var label = new GameObject("Label", typeof(Text));
            label.transform.SetParent(settingObj.transform, false);
            var labelText = label.GetComponent<Text>();
            labelText.text = "受伤波持续时间 (秒):";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 0.5f);
            labelRect.anchorMax = new(0.4f, 0.5f);
            labelRect.pivot = new(0, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            var sliderObj = new GameObject("Slider", typeof(Slider));
            sliderObj.transform.SetParent(settingObj.transform, false);
            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 1;
            slider.maxValue = 5;
            slider.wholeNumbers = true;
            slider.value = _config.HurtDuration;
            slider.onValueChanged.AddListener(OnHurtDurationChanged);
            _hurtDurationSlider = slider;

            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new(0.4f, 0.5f);
            sliderRect.anchorMax = new(0.85f, 0.5f);
            sliderRect.pivot = new(0, 0.5f);
            sliderRect.anchoredPosition = new(10, 0);
            sliderRect.sizeDelta = new(0, 20);

            var valueText = new GameObject("ValueText", typeof(Text));
            valueText.transform.SetParent(settingObj.transform, false);
            var valueTextComponent = valueText.GetComponent<Text>();
            valueTextComponent.text = _config.HurtDuration.ToString();
            valueTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            valueTextComponent.fontSize = 14;
            valueTextComponent.color = Color.white;
            valueTextComponent.alignment = TextAnchor.MiddleCenter;
            _hurtDurationText = valueTextComponent;

            var valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new(0.85f, 0.5f);
            valueRect.anchorMax = new(1, 0.5f);
            valueRect.pivot = new(0, 0.5f);
            valueRect.anchoredPosition = new(10, 0);
            valueRect.sizeDelta = new(0, 20);
        }

        private void BuildHurtWaveTypeSetting(GameObject parent)
        {
            if (_config == null) return;

            var settingObj = new GameObject("HurtWaveTypeSetting", typeof(RectTransform));
            settingObj.transform.SetParent(parent.transform, false);
            var settingRect = settingObj.GetComponent<RectTransform>();
            settingRect.sizeDelta = new(0, 50);

            var label = new GameObject("Label", typeof(Text));
            label.transform.SetParent(settingObj.transform, false);
            var labelText = label.GetComponent<Text>();
            labelText.text = "受伤波类型:";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 0.5f);
            labelRect.anchorMax = new(0.4f, 0.5f);
            labelRect.pivot = new(0, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            var dropdownObj = new GameObject("Dropdown", typeof(Dropdown));
            dropdownObj.transform.SetParent(settingObj.transform, false);
            var dropdown = dropdownObj.GetComponent<Dropdown>();
            var options = new List<string> { "默认" };
            options.AddRange(CustomWaveManager.GetAllCustomWaveNames());
            dropdown.options = options.Select(o => new Dropdown.OptionData(o)).ToList();
            var currentIndex = string.IsNullOrEmpty(_config.HurtWaveType)
                ? 0
                : options.FindIndex(o => o.Equals(_config.HurtWaveType, StringComparison.OrdinalIgnoreCase));
            if (currentIndex < 0) currentIndex = 0;
            dropdown.value = currentIndex;
            dropdown.onValueChanged.AddListener(OnHurtWaveTypeChanged);
            _hurtWaveTypeDropdown = dropdown;

            var dropdownRect = dropdownObj.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new(0.4f, 0.5f);
            dropdownRect.anchorMax = new(1, 0.5f);
            dropdownRect.pivot = new(0, 0.5f);
            dropdownRect.anchoredPosition = new(10, 0);
            dropdownRect.sizeDelta = new(0, 30);
        }

        private void BuildDeathDurationSetting(GameObject parent)
        {
            if (_config == null) return;

            var settingObj = new GameObject("DeathDurationSetting", typeof(RectTransform));
            settingObj.transform.SetParent(parent.transform, false);
            var settingRect = settingObj.GetComponent<RectTransform>();
            settingRect.sizeDelta = new(0, 50);

            var label = new GameObject("Label", typeof(Text));
            label.transform.SetParent(settingObj.transform, false);
            var labelText = label.GetComponent<Text>();
            labelText.text = "死亡波持续时间 (秒):";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 0.5f);
            labelRect.anchorMax = new(0.4f, 0.5f);
            labelRect.pivot = new(0, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            var sliderObj = new GameObject("Slider", typeof(Slider));
            sliderObj.transform.SetParent(settingObj.transform, false);
            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 1;
            slider.maxValue = 5;
            slider.wholeNumbers = true;
            slider.value = _config.DeathDuration;
            slider.onValueChanged.AddListener(OnDeathDurationChanged);
            _deathDurationSlider = slider;

            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new(0.4f, 0.5f);
            sliderRect.anchorMax = new(0.85f, 0.5f);
            sliderRect.pivot = new(0, 0.5f);
            sliderRect.anchoredPosition = new(10, 0);
            sliderRect.sizeDelta = new(0, 20);

            var valueText = new GameObject("ValueText", typeof(Text));
            valueText.transform.SetParent(settingObj.transform, false);
            var valueTextComponent = valueText.GetComponent<Text>();
            valueTextComponent.text = _config.DeathDuration.ToString();
            valueTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            valueTextComponent.fontSize = 14;
            valueTextComponent.color = Color.white;
            valueTextComponent.alignment = TextAnchor.MiddleCenter;
            _deathDurationText = valueTextComponent;

            var valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new(0.85f, 0.5f);
            valueRect.anchorMax = new(1, 0.5f);
            valueRect.pivot = new(0, 0.5f);
            valueRect.anchoredPosition = new(10, 0);
            valueRect.sizeDelta = new(0, 20);
        }

        private void BuildDeathWaveTypeSetting(GameObject parent)
        {
            if (_config == null) return;

            var settingObj = new GameObject("DeathWaveTypeSetting", typeof(RectTransform));
            settingObj.transform.SetParent(parent.transform, false);
            var settingRect = settingObj.GetComponent<RectTransform>();
            settingRect.sizeDelta = new(0, 50);

            var label = new GameObject("Label", typeof(Text));
            label.transform.SetParent(settingObj.transform, false);
            var labelText = label.GetComponent<Text>();
            labelText.text = "死亡波类型:";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 0.5f);
            labelRect.anchorMax = new(0.4f, 0.5f);
            labelRect.pivot = new(0, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            var dropdownObj = new GameObject("Dropdown", typeof(Dropdown));
            dropdownObj.transform.SetParent(settingObj.transform, false);
            var dropdown = dropdownObj.GetComponent<Dropdown>();
            var options = new List<string> { "默认" };
            options.AddRange(CustomWaveManager.GetAllCustomWaveNames());
            dropdown.options = options.Select(o => new Dropdown.OptionData(o)).ToList();
            var currentIndex = string.IsNullOrEmpty(_config.DeathWaveType)
                ? 0
                : options.FindIndex(o => o.Equals(_config.DeathWaveType, StringComparison.OrdinalIgnoreCase));
            if (currentIndex < 0) currentIndex = 0;
            dropdown.value = currentIndex;
            dropdown.onValueChanged.AddListener(OnDeathWaveTypeChanged);
            _deathWaveTypeDropdown = dropdown;

            var dropdownRect = dropdownObj.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new(0.4f, 0.5f);
            dropdownRect.anchorMax = new(1, 0.5f);
            dropdownRect.pivot = new(0, 0.5f);
            dropdownRect.anchoredPosition = new(10, 0);
            dropdownRect.sizeDelta = new(0, 30);
        }

        private void BuildDefaultStrengthSetting(GameObject parent)
        {
            if (_config == null) return;

            var settingObj = new GameObject("DefaultStrengthSetting", typeof(RectTransform));
            settingObj.transform.SetParent(parent.transform, false);
            var settingRect = settingObj.GetComponent<RectTransform>();
            settingRect.sizeDelta = new(0, 50);

            var label = new GameObject("Label", typeof(Text));
            label.transform.SetParent(settingObj.transform, false);
            var labelText = label.GetComponent<Text>();
            labelText.text = "默认电流强度:";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 0.5f);
            labelRect.anchorMax = new(0.4f, 0.5f);
            labelRect.pivot = new(0, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            var sliderObj = new GameObject("Slider", typeof(Slider));
            sliderObj.transform.SetParent(settingObj.transform, false);
            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.value = _config.DefaultStrength;
            slider.onValueChanged.AddListener(OnDefaultStrengthChanged);
            _defaultStrengthSlider = slider;

            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new(0.4f, 0.5f);
            sliderRect.anchorMax = new(0.85f, 0.5f);
            sliderRect.pivot = new(0, 0.5f);
            sliderRect.anchoredPosition = new(10, 0);
            sliderRect.sizeDelta = new(0, 20);

            var valueText = new GameObject("ValueText", typeof(Text));
            valueText.transform.SetParent(settingObj.transform, false);
            var valueTextComponent = valueText.GetComponent<Text>();
            valueTextComponent.text = _config.DefaultStrength.ToString();
            valueTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            valueTextComponent.fontSize = 14;
            valueTextComponent.color = Color.white;
            valueTextComponent.alignment = TextAnchor.MiddleCenter;
            _defaultStrengthText = valueTextComponent;

            var valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new(0.85f, 0.5f);
            valueRect.anchorMax = new(1, 0.5f);
            valueRect.pivot = new(0, 0.5f);
            valueRect.anchoredPosition = new(10, 0);
            valueRect.sizeDelta = new(0, 20);
        }

        private void BuildMaxStrengthSetting(GameObject parent)
        {
            if (_config == null) return;

            var settingObj = new GameObject("MaxStrengthSetting", typeof(RectTransform));
            settingObj.transform.SetParent(parent.transform, false);
            var settingRect = settingObj.GetComponent<RectTransform>();
            settingRect.sizeDelta = new(0, 50);

            var label = new GameObject("Label", typeof(Text));
            label.transform.SetParent(settingObj.transform, false);
            var labelText = label.GetComponent<Text>();
            labelText.text = "最大电流强度:";
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new(0, 0.5f);
            labelRect.anchorMax = new(0.4f, 0.5f);
            labelRect.pivot = new(0, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            var sliderObj = new GameObject("Slider", typeof(Slider));
            sliderObj.transform.SetParent(settingObj.transform, false);
            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.wholeNumbers = true;
            slider.value = _config.MaxStrength;
            slider.onValueChanged.AddListener(OnMaxStrengthChanged);
            _maxStrengthSlider = slider;

            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new(0.4f, 0.5f);
            sliderRect.anchorMax = new(0.85f, 0.5f);
            sliderRect.pivot = new(0, 0.5f);
            sliderRect.anchoredPosition = new(10, 0);
            sliderRect.sizeDelta = new(0, 20);

            var valueText = new GameObject("ValueText", typeof(Text));
            valueText.transform.SetParent(settingObj.transform, false);
            var valueTextComponent = valueText.GetComponent<Text>();
            valueTextComponent.text = _config.MaxStrength.ToString();
            valueTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            valueTextComponent.fontSize = 14;
            valueTextComponent.color = Color.white;
            valueTextComponent.alignment = TextAnchor.MiddleCenter;
            _maxStrengthText = valueTextComponent;

            var valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new(0.85f, 0.5f);
            valueRect.anchorMax = new(1, 0.5f);
            valueRect.pivot = new(0, 0.5f);
            valueRect.anchoredPosition = new(10, 0);
            valueRect.sizeDelta = new(0, 20);
        }

        private void BuildCloseButton()
        {
            if (_panelRoot == null) return;

            var closeButton = new GameObject("CloseButton", typeof(Image), typeof(Button));
            closeButton.transform.SetParent(_panelRoot.transform, false);
            var closeImage = closeButton.GetComponent<Image>();
            closeImage.color = new(0.2f, 0.2f, 0.2f, 1);

            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new(1, 1);
            closeRect.anchorMax = new(1, 1);
            closeRect.pivot = new(1, 1);
            closeRect.anchoredPosition = new(-10, -10);
            closeRect.sizeDelta = new(30, 30);

            var closeText = new GameObject("Text", typeof(Text));
            closeText.transform.SetParent(closeButton.transform, false);
            var textComponent = closeText.GetComponent<Text>();
            textComponent.text = "×";
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 20;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            var textRect = closeText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var button = closeButton.GetComponent<Button>();
            button.onClick.AddListener(HidePanel);
            _closeButton = button;
        }

        private void OnHurtDurationChanged(float value)
        {
            if (_config == null) return;
            var intValue = (int)value;
            _config.HurtDuration = intValue;
            if (_hurtDurationText != null) _hurtDurationText.text = intValue.ToString();
            SaveConfig();
        }

        private void OnHurtWaveTypeChanged(int index)
        {
            if (_config == null || _hurtWaveTypeDropdown == null) return;
            var options = _hurtWaveTypeDropdown.options;
            if (index < 0 || index >= options.Count) return;
            var selected = options[index].text;
            _config.HurtWaveType = selected == "默认" ? null : selected;
            SaveConfig();
        }

        private void OnDeathDurationChanged(float value)
        {
            if (_config == null) return;
            var intValue = (int)value;
            _config.DeathDuration = intValue;
            if (_deathDurationText != null) _deathDurationText.text = intValue.ToString();
            SaveConfig();
        }

        private void OnDeathWaveTypeChanged(int index)
        {
            if (_config == null || _deathWaveTypeDropdown == null) return;
            var options = _deathWaveTypeDropdown.options;
            if (index < 0 || index >= options.Count) return;
            var selected = options[index].text;
            _config.DeathWaveType = selected == "默认" ? null : selected;
            SaveConfig();
        }

        private void OnDefaultStrengthChanged(float value)
        {
            if (_config == null) return;
            var intValue = (int)value;
            _config.DefaultStrength = intValue;
            if (_defaultStrengthText != null) _defaultStrengthText.text = intValue.ToString();
            SaveConfig();
        }

        private void OnMaxStrengthChanged(float value)
        {
            if (_config == null) return;
            var intValue = (int)value;
            _config.MaxStrength = intValue;
            if (_maxStrengthText != null) _maxStrengthText.text = intValue.ToString();
            SaveConfig();
        }

        private void SaveConfig()
        {
            if (_config == null) return;
            ConfigManager.SaveConfigToFile(_config, "DGLabConfig.json");
        }

        private void HandleKeyInputCapture()
        {
            if (!_isWaitingForKeyInput) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _isWaitingForKeyInput = false;
                return;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode == KeyCode.Mouse0 || keyCode == KeyCode.Mouse1 || keyCode == KeyCode.Mouse2 ||
                        keyCode == KeyCode.Mouse3 || keyCode == KeyCode.Mouse4 || keyCode == KeyCode.Mouse5 ||
                        keyCode == KeyCode.Mouse6)
                        continue;

                    _toggleKey = keyCode;
                    _isWaitingForKeyInput = false;
                    ModLogger.Log($"Toggle key set to: {keyCode}");
                    return;
                }
        }

        private static bool IsTypingInInputField()
        {
            var current = EventSystem.current;
            if (current == null || current.currentSelectedGameObject == null) return false;

            var inputField = current.currentSelectedGameObject.GetComponent<InputField>();
            return inputField != null && inputField.isFocused;
        }

        private void ShowPanel()
        {
            if (!_isInitialized || _panelRoot == null)
            {
                ModLogger.LogWarning("Cannot show panel - not initialized!");
                return;
            }

            _uiActive = true;
            if (_overlay != null) _overlay.SetActive(true);

            if (_panelRoot != null) _panelRoot.SetActive(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var current = EventSystem.current;
            if (current == null)
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                DontDestroyOnLoad(eventSystem);
            }

            _charInput = FindObjectOfType<CharacterInputControl>();
            if (_charInput != null)
            {
                _charInput.enabled = false;
                ModLogger.Log("CharacterInputControl disabled.");
            }

            _playerInput = FindObjectOfType<PlayerInput>();
            if (_playerInput != null)
            {
                _playerInput.DeactivateInput();
                ModLogger.Log("PlayerInput deactivated (game input blocked).");
            }

            var allBehaviours = FindObjectsOfType<MonoBehaviour>();
            foreach (var behaviour in allBehaviours)
            {
                var type = behaviour.GetType();
                if (!type.Name.Contains("CameraController") && !type.Name.Contains("MouseLook")) continue;
                behaviour.enabled = false;
                _cameraController = behaviour;
                _cameraLockDisabled = true;
                ModLogger.Log($"Camera controller disabled: {type.FullName}");
                break;
            }

            StartCoroutine(ForceCursorFree());
            ModLogger.Log("DGLab config panel opened.");
        }

        private void HidePanel()
        {
            _uiActive = false;
            StopAllCoroutines();

            if (_overlay != null) _overlay.SetActive(false);

            if (_panelRoot != null) _panelRoot.SetActive(false);

            if (_charInput != null)
            {
                _charInput.enabled = true;
                _charInput = null;
                ModLogger.Log("CharacterInputControl re-enabled.");
            }

            if (_playerInput != null)
            {
                _playerInput.ActivateInput();
                _playerInput = null;
                ModLogger.Log("PlayerInput reactivated (game input restored).");
            }

            if (_cameraLockDisabled && _cameraController != null)
            {
                _cameraController.enabled = true;
                _cameraController = null;
                _cameraLockDisabled = false;
                ModLogger.Log("Camera controller re-enabled.");
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            ModLogger.Log("DGLab config panel closed.");
        }

        private IEnumerator ForceCursorFree()
        {
            while (_uiActive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                yield return null;
            }
        }
    }
}