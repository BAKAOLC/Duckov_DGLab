using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Duckov_DGLab.Configs;
using UnityEngine;
using UnityEngine.Events;
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
        private InputField? _deathDurationInput;
        private Slider? _deathDurationSlider;
        private Dropdown? _deathWaveTypeDropdown;
        private InputField? _defaultStrengthInput;
        private Slider? _defaultStrengthSlider;
        private InputField? _hurtDurationInput;
        private Slider? _hurtDurationSlider;
        private Dropdown? _hurtWaveTypeDropdown;
        private bool _isInitialized;
        private bool _isWaitingForKeyInput;
        private GameObject? _overlay;
        private GameObject? _panelRoot;
        private PlayerInput? _playerInput;
        private KeyCode _toggleKey = KeyCode.N;
        private Button? _toggleKeyButton;
        private Text? _toggleKeyDisplayText;
        private bool _uiActive;
        private GameObject? _uiRoot;

        private void Start()
        {
            _config = ModBehaviour.Instance?.Config;
            if (_config != null) _toggleKey = _config.ToggleKey;
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

            if (_config != null) _toggleKey = _config.ToggleKey;

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
            layoutGroup.padding = new(20, 20, 20, 20);
            layoutGroup.spacing = 20;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            BuildToggleKeySetting(contentArea);
            BuildHurtDurationSetting(contentArea);
            BuildHurtWaveTypeSetting(contentArea);
            BuildDeathDurationSetting(contentArea);
            BuildDeathWaveTypeSetting(contentArea);
            BuildDefaultStrengthSetting(contentArea);
        }

        private static GameObject CreateSettingRow(GameObject parent, string name)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent.transform, false);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new(0, 40);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 15;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.padding = new(0, 0, 0, 0);

            return row;
        }

        private static Text CreateLabel(GameObject parent, string text, float width = 180)
        {
            var label = new GameObject("Label", typeof(Text));
            label.transform.SetParent(parent.transform, false);
            var labelText = label.GetComponent<Text>();
            labelText.text = text;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.sizeDelta = new(width, 0);
            return labelText;
        }

        private static Slider CreateSlider(GameObject parent, float minValue, float maxValue, float currentValue,
            UnityAction<float> onValueChanged, float width = 250)
        {
            var sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent.transform, false);
            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.wholeNumbers = true;
            slider.value = currentValue;
            slider.onValueChanged.AddListener(onValueChanged);

            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new(width, 20);

            var background = new GameObject("Background", typeof(Image));
            background.transform.SetParent(sliderObj.transform, false);
            var bgImage = background.GetComponent<Image>();
            bgImage.color = new(0.2f, 0.2f, 0.2f, 1);
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            slider.targetGraphic = bgImage;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.anchoredPosition = Vector2.zero;
            fillAreaRect.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill", typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.GetComponent<Image>();
            fillImage.color = new(0.2f, 0.6f, 0.9f, 1);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new(1, 1);
            fillRect.sizeDelta = Vector2.zero;
            slider.fillRect = fillRect;

            var handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleSlideArea.transform.SetParent(sliderObj.transform, false);
            var handleAreaRect = handleSlideArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = Vector2.zero;
            handleAreaRect.anchoredPosition = Vector2.zero;

            var handle = new GameObject("Handle", typeof(Image));
            handle.transform.SetParent(handleSlideArea.transform, false);
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = Color.white;
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new(16, 16);
            slider.handleRect = handleRect;

            return slider;
        }

        private static InputField CreateInputField(GameObject parent, string text,
            UnityAction<string> onValueChanged, float width = 80)
        {
            var inputObj = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObj.transform.SetParent(parent.transform, false);
            var inputField = inputObj.GetComponent<InputField>();
            var inputImage = inputObj.GetComponent<Image>();
            inputImage.color = new(0.15f, 0.15f, 0.15f, 1);

            var inputRect = inputObj.GetComponent<RectTransform>();
            inputRect.sizeDelta = new(width, 25);

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputObj.transform, false);
            var textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new(5, 0);
            textAreaRect.offsetMax = new(-5, 0);

            var placeholder = new GameObject("Placeholder", typeof(Text));
            placeholder.transform.SetParent(textArea.transform, false);
            var placeholderText = placeholder.GetComponent<Text>();
            placeholderText.text = "";
            placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholderText.fontSize = 14;
            placeholderText.color = new(0.5f, 0.5f, 0.5f, 1);
            placeholderText.alignment = TextAnchor.MiddleCenter;
            var placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            inputField.placeholder = placeholderText;

            var textObj = new GameObject("Text", typeof(Text));
            textObj.transform.SetParent(textArea.transform, false);
            var textComponent = textObj.GetComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            inputField.textComponent = textComponent;

            inputField.contentType = InputField.ContentType.IntegerNumber;
            inputField.text = text;
            inputField.onValueChanged.AddListener(onValueChanged);

            return inputField;
        }


        private static Dropdown CreateDropdown(GameObject parent, List<string> options, int currentIndex,
            UnityAction<int> onValueChanged, float width = 350)
        {
            var dropdownObj = new GameObject("Dropdown", typeof(RectTransform), typeof(Dropdown));
            dropdownObj.transform.SetParent(parent.transform, false);
            var dropdown = dropdownObj.GetComponent<Dropdown>();
            dropdown.options = options.Select(o => new Dropdown.OptionData(o)).ToList();
            dropdown.value = currentIndex;
            dropdown.onValueChanged.AddListener(onValueChanged);

            var dropdownRect = dropdownObj.GetComponent<RectTransform>();
            dropdownRect.sizeDelta = new(width, 30);

            var dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new(0.2f, 0.2f, 0.2f, 1);
            dropdown.targetGraphic = dropdownImage;

            var dropdownLabel = new GameObject("Label", typeof(Text));
            dropdownLabel.transform.SetParent(dropdownObj.transform, false);
            var labelTextComponent = dropdownLabel.GetComponent<Text>();
            labelTextComponent.text = options[currentIndex];
            labelTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelTextComponent.fontSize = 14;
            labelTextComponent.color = Color.white;
            labelTextComponent.alignment = TextAnchor.MiddleLeft;
            var labelRectTransform = dropdownLabel.GetComponent<RectTransform>();
            labelRectTransform.anchorMin = Vector2.zero;
            labelRectTransform.anchorMax = Vector2.one;
            labelRectTransform.offsetMin = new(10, 0);
            labelRectTransform.offsetMax = new(-25, 0);
            dropdown.captionText = labelTextComponent;

            var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(dropdownObj.transform, false);
            template.SetActive(false);
            var templateImage = template.GetComponent<Image>();
            templateImage.color = new(0.15f, 0.15f, 0.15f, 1);
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new(0, 0);
            templateRect.anchorMax = new(1, 0);
            templateRect.pivot = new(0.5f, 1);
            templateRect.anchoredPosition = new(0, 2);
            templateRect.sizeDelta = new(0, 150);
            dropdown.template = templateRect;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            var viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new(0.15f, 0.15f, 0.15f, 1);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new(0, 1);
            contentRect.anchorMax = new(1, 1);
            contentRect.pivot = new(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new(0, 0);

            var contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            var contentSizeFitter = content.GetComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.sizeDelta = new(0, 30);
            var itemToggle = item.GetComponent<Toggle>();

            var itemBackground = new GameObject("Item Background", typeof(Image));
            itemBackground.transform.SetParent(item.transform, false);
            var itemBgImage = itemBackground.GetComponent<Image>();
            itemBgImage.color = new(0.2f, 0.2f, 0.2f, 1);
            var itemBgRect = itemBackground.GetComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;
            itemToggle.targetGraphic = itemBgImage;

            var itemLabel = new GameObject("Item Label", typeof(Text));
            itemLabel.transform.SetParent(item.transform, false);
            var itemLabelText = itemLabel.GetComponent<Text>();
            itemLabelText.text = "选项";
            itemLabelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            itemLabelText.fontSize = 14;
            itemLabelText.color = Color.white;
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            var itemLabelRect = itemLabel.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new(10, 0);
            itemLabelRect.offsetMax = new(-10, 0);
            itemToggle.graphic = itemLabelText;

            var scrollRect = template.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar = null;
            scrollRect.horizontalScrollbar = null;

            dropdown.itemText = itemLabelText;

            return dropdown;
        }

        private void BuildHurtDurationSetting(GameObject parent)
        {
            if (_config == null) return;

            var row = CreateSettingRow(parent, "HurtDurationSetting");
            CreateLabel(row, "受伤波持续时间 (秒):");
            _hurtDurationSlider = CreateSlider(row, 1, 5, _config.HurtDuration, OnHurtDurationChanged);
            _hurtDurationInput = CreateInputField(row, _config.HurtDuration.ToString(), OnHurtDurationInputChanged);
        }

        private void BuildHurtWaveTypeSetting(GameObject parent)
        {
            if (_config == null) return;

            var row = CreateSettingRow(parent, "HurtWaveTypeSetting");
            CreateLabel(row, "受伤波类型:");
            var options = new List<string> { "默认" };
            options.AddRange(CustomWaveManager.GetAllCustomWaveNames());
            var currentIndex = string.IsNullOrEmpty(_config.HurtWaveType)
                ? 0
                : options.FindIndex(o => o.Equals(_config.HurtWaveType, StringComparison.OrdinalIgnoreCase));
            if (currentIndex < 0) currentIndex = 0;
            _hurtWaveTypeDropdown = CreateDropdown(row, options, currentIndex, OnHurtWaveTypeChanged);
        }

        private void BuildDeathDurationSetting(GameObject parent)
        {
            if (_config == null) return;

            var row = CreateSettingRow(parent, "DeathDurationSetting");
            CreateLabel(row, "死亡波持续时间 (秒):");
            _deathDurationSlider = CreateSlider(row, 1, 5, _config.DeathDuration, OnDeathDurationChanged);
            _deathDurationInput =
                CreateInputField(row, _config.DeathDuration.ToString(), OnDeathDurationInputChanged);
        }

        private void BuildDeathWaveTypeSetting(GameObject parent)
        {
            if (_config == null) return;

            var row = CreateSettingRow(parent, "DeathWaveTypeSetting");
            CreateLabel(row, "死亡波类型:");
            var options = new List<string> { "默认" };
            options.AddRange(CustomWaveManager.GetAllCustomWaveNames());
            var currentIndex = string.IsNullOrEmpty(_config.DeathWaveType)
                ? 0
                : options.FindIndex(o => o.Equals(_config.DeathWaveType, StringComparison.OrdinalIgnoreCase));
            if (currentIndex < 0) currentIndex = 0;
            _deathWaveTypeDropdown = CreateDropdown(row, options, currentIndex, OnDeathWaveTypeChanged);
        }

        private void BuildToggleKeySetting(GameObject parent)
        {
            if (_config == null) return;

            var row = CreateSettingRow(parent, "ToggleKeySetting");
            CreateLabel(row, "打开/关闭界面按键:");

            var buttonObj = new GameObject("ToggleKeyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(row.transform, false);
            var buttonImage = buttonObj.GetComponent<Image>();
            buttonImage.color = new(0.2f, 0.2f, 0.2f, 1);
            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new(200, 30);

            var buttonText = new GameObject("Text", typeof(Text));
            buttonText.transform.SetParent(buttonObj.transform, false);
            var textComponent = buttonText.GetComponent<Text>();
            textComponent.text = _config.ToggleKey.ToString();
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            var textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var button = buttonObj.GetComponent<Button>();
            button.onClick.AddListener(OnToggleKeyButtonClicked);
            _toggleKeyButton = button;
            _toggleKeyDisplayText = textComponent;
        }

        private void BuildDefaultStrengthSetting(GameObject parent)
        {
            if (_config == null) return;

            var row = CreateSettingRow(parent, "DefaultStrengthSetting");
            CreateLabel(row, "默认电流强度:");
            _defaultStrengthSlider = CreateSlider(row, 0, 100, _config.DefaultStrength, OnDefaultStrengthChanged);
            _defaultStrengthInput =
                CreateInputField(row, _config.DefaultStrength.ToString(), OnDefaultStrengthInputChanged);
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
            if (_hurtDurationInput != null) _hurtDurationInput.text = intValue.ToString();
            SaveConfig();
        }

        private void OnHurtDurationInputChanged(string value)
        {
            if (_config == null || string.IsNullOrEmpty(value)) return;
            if (int.TryParse(value, out var intValue))
            {
                intValue = Mathf.Clamp(intValue, 1, 5);
                _config.HurtDuration = intValue;
                if (_hurtDurationSlider != null) _hurtDurationSlider.value = intValue;
                if (_hurtDurationInput != null) _hurtDurationInput.text = intValue.ToString();
                SaveConfig();
            }
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
            if (_deathDurationInput != null) _deathDurationInput.text = intValue.ToString();
            SaveConfig();
        }

        private void OnDeathDurationInputChanged(string value)
        {
            if (_config == null || string.IsNullOrEmpty(value)) return;
            if (int.TryParse(value, out var intValue))
            {
                intValue = Mathf.Clamp(intValue, 1, 5);
                _config.DeathDuration = intValue;
                if (_deathDurationSlider != null) _deathDurationSlider.value = intValue;
                if (_deathDurationInput != null) _deathDurationInput.text = intValue.ToString();
                SaveConfig();
            }
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
            if (_defaultStrengthInput != null) _defaultStrengthInput.text = intValue.ToString();
            SaveConfig();
        }

        private void OnDefaultStrengthInputChanged(string value)
        {
            if (_config == null || string.IsNullOrEmpty(value)) return;
            if (int.TryParse(value, out var intValue))
            {
                intValue = Mathf.Clamp(intValue, 0, 100);
                _config.DefaultStrength = intValue;
                if (_defaultStrengthSlider != null) _defaultStrengthSlider.value = intValue;
                if (_defaultStrengthInput != null) _defaultStrengthInput.text = intValue.ToString();
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            if (_config == null) return;
            ConfigManager.SaveConfigToFile(_config, "DGLabConfig.json");
        }

        private void OnToggleKeyButtonClicked()
        {
            if (_isWaitingForKeyInput) return;
            _isWaitingForKeyInput = true;
            if (_toggleKeyDisplayText != null)
            {
                _toggleKeyDisplayText.text = "按任意键...";
                _toggleKeyDisplayText.color = new(1f, 0.8f, 0f, 1f);
            }
        }

        private void HandleKeyInputCapture()
        {
            if (!_isWaitingForKeyInput) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _isWaitingForKeyInput = false;
                UpdateToggleKeyDisplay();
                return;
            }

            foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                if (Input.GetKeyDown(keyCode))
                {
                    if (keyCode is KeyCode.Mouse0 or KeyCode.Mouse1 or KeyCode.Mouse2 or KeyCode.Mouse3
                        or KeyCode.Mouse4 or KeyCode.Mouse5 or KeyCode.Mouse6)
                        continue;

                    _toggleKey = keyCode;
                    if (_config != null)
                    {
                        _config.ToggleKey = keyCode;
                        SaveConfig();
                    }

                    _isWaitingForKeyInput = false;
                    UpdateToggleKeyDisplay();
                    ModLogger.Log($"Toggle key set to: {keyCode}");
                    return;
                }
        }

        private void UpdateToggleKeyDisplay()
        {
            if (_toggleKeyDisplayText == null) return;
            _toggleKeyDisplayText.text = _toggleKey.ToString();
            _toggleKeyDisplayText.color = Color.white;
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