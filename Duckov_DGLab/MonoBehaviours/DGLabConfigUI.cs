using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DGLabCSharp.Enums;
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
        private Text? _connectedAppsText;
        private Text? _connectionStatusText;
        private int _currentPage;
        private Text? _currentStrengthText;
        private InputField? _deathDurationInput;
        private Slider? _deathDurationSlider;
        private Dropdown? _deathWaveTypeDropdown;
        private InputField? _hurtDurationInput;
        private Slider? _hurtDurationSlider;
        private Dropdown? _hurtWaveTypeDropdown;
        private bool _isInitialized;
        private bool _isWaitingForKeyInput;
        private GameObject? _overlay;
        private GameObject? _panelRoot;
        private PlayerInput? _playerInput;
        private RawImage? _qrCodeImage;
        private GameObject? _qrCodePanel;
        private GameObject? _settingsPage;
        private Button? _settingsTabButton;
        private GameObject? _statusPage;
        private Button? _statusTabButton;
        private InputField? _strengthAInput;
        private Slider? _strengthASlider;
        private InputField? _strengthBInput;
        private Slider? _strengthBSlider;
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
            BuildTabs();
            BuildContent();
            BuildCloseButton();
            _currentPage = 0;
            SwitchPage(0);
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

        private void BuildTabs()
        {
            if (_panelRoot == null) return;

            var tabsArea = new GameObject("TabsArea", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsArea.transform.SetParent(_panelRoot.transform, false);
            var tabsRect = tabsArea.GetComponent<RectTransform>();
            tabsRect.anchorMin = new(0, 1);
            tabsRect.anchorMax = new(1, 1);
            tabsRect.pivot = new(0.5f, 1);
            tabsRect.anchoredPosition = new(0, -60);
            tabsRect.sizeDelta = new(0, 40);

            var tabsLayout = tabsArea.GetComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 10;
            tabsLayout.padding = new(20, 20, 0, 0);
            tabsLayout.childControlWidth = false;
            tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandWidth = false;
            tabsLayout.childForceExpandHeight = true;

            _settingsTabButton = CreateTabButton(tabsArea, "设置", 0);
            _statusTabButton = CreateTabButton(tabsArea, "状态", 1);
        }

        private Button CreateTabButton(GameObject parent, string text, int pageIndex)
        {
            var buttonObj = new GameObject($"TabButton_{text}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(parent.transform, false);
            var buttonImage = buttonObj.GetComponent<Image>();
            buttonImage.color = new(0.2f, 0.2f, 0.2f, 1);
            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new(120, 35);

            var buttonText = new GameObject("Text", typeof(Text));
            buttonText.transform.SetParent(buttonObj.transform, false);
            var textComponent = buttonText.GetComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComponent.fontSize = 16;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            var textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var button = buttonObj.GetComponent<Button>();
            button.onClick.AddListener(() => SwitchPage(pageIndex));

            return button;
        }

        private void BuildContent()
        {
            if (_panelRoot == null || _config == null) return;

            var contentArea = new GameObject("ContentArea", typeof(RectTransform));
            contentArea.transform.SetParent(_panelRoot.transform, false);
            var contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = new(0, 0);
            contentRect.anchorMax = new(1, 1);
            contentRect.offsetMin = new(20, 20);
            contentRect.offsetMax = new(-20, -100);

            _settingsPage = new("SettingsPage", typeof(RectTransform));
            _settingsPage.transform.SetParent(contentArea.transform, false);
            var settingsRect = _settingsPage.GetComponent<RectTransform>();
            settingsRect.anchorMin = Vector2.zero;
            settingsRect.anchorMax = Vector2.one;
            settingsRect.sizeDelta = Vector2.zero;

            var settingsLayout = _settingsPage.AddComponent<VerticalLayoutGroup>();
            settingsLayout.padding = new(20, 20, 20, 20);
            settingsLayout.spacing = 20;
            settingsLayout.childAlignment = TextAnchor.UpperLeft;
            settingsLayout.childControlWidth = true;
            settingsLayout.childControlHeight = false;
            settingsLayout.childForceExpandWidth = true;
            settingsLayout.childForceExpandHeight = false;

            BuildToggleKeySetting(_settingsPage);
            BuildHurtDurationSetting(_settingsPage);
            BuildHurtWaveTypeSetting(_settingsPage);
            BuildDeathDurationSetting(_settingsPage);
            BuildDeathWaveTypeSetting(_settingsPage);

            _statusPage = new("StatusPage", typeof(RectTransform));
            _statusPage.transform.SetParent(contentArea.transform, false);
            var statusRect = _statusPage.GetComponent<RectTransform>();
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.sizeDelta = Vector2.zero;

            BuildStatusPage();
        }

        private void BuildStatusPage()
        {
            if (_statusPage == null) return;

            var statusLayout = _statusPage.AddComponent<VerticalLayoutGroup>();
            statusLayout.padding = new(20, 20, 20, 20);
            statusLayout.spacing = 20;
            statusLayout.childAlignment = TextAnchor.UpperLeft;
            statusLayout.childControlWidth = true;
            statusLayout.childControlHeight = false;
            statusLayout.childForceExpandWidth = true;
            statusLayout.childForceExpandHeight = false;

            var connectionStatusRow = CreateSettingRow(_statusPage, "ConnectionStatusRow");
            CreateLabel(connectionStatusRow, "连接状态:");
            var connectionStatusLabel = new GameObject("ConnectionStatusLabel", typeof(Text));
            connectionStatusLabel.transform.SetParent(connectionStatusRow.transform, false);
            _connectionStatusText = connectionStatusLabel.GetComponent<Text>();
            _connectionStatusText.text = "未初始化";
            _connectionStatusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _connectionStatusText.fontSize = 14;
            _connectionStatusText.color = Color.white;
            _connectionStatusText.alignment = TextAnchor.MiddleLeft;
            var connectionStatusRect = connectionStatusLabel.GetComponent<RectTransform>();
            connectionStatusRect.sizeDelta = new(200, 0);

            var connectedAppsRow = CreateSettingRow(_statusPage, "ConnectedAppsRow");
            CreateLabel(connectedAppsRow, "已连接App数量:");
            var connectedAppsLabel = new GameObject("ConnectedAppsLabel", typeof(Text));
            connectedAppsLabel.transform.SetParent(connectedAppsRow.transform, false);
            _connectedAppsText = connectedAppsLabel.GetComponent<Text>();
            _connectedAppsText.text = "0";
            _connectedAppsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _connectedAppsText.fontSize = 14;
            _connectedAppsText.color = Color.white;
            _connectedAppsText.alignment = TextAnchor.MiddleLeft;
            var connectedAppsRect = connectedAppsLabel.GetComponent<RectTransform>();
            connectedAppsRect.sizeDelta = new(200, 0);

            var currentStrengthRow = CreateSettingRow(_statusPage, "CurrentStrengthRow");
            CreateLabel(currentStrengthRow, "当前强度:");
            var currentStrengthLabel = new GameObject("CurrentStrengthLabel", typeof(Text));
            currentStrengthLabel.transform.SetParent(currentStrengthRow.transform, false);
            _currentStrengthText = currentStrengthLabel.GetComponent<Text>();
            _currentStrengthText.text = "未获取";
            _currentStrengthText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _currentStrengthText.fontSize = 14;
            _currentStrengthText.color = Color.white;
            _currentStrengthText.alignment = TextAnchor.MiddleLeft;
            var currentStrengthRect = currentStrengthLabel.GetComponent<RectTransform>();
            currentStrengthRect.sizeDelta = new(200, 0);

            var strengthARow = CreateSettingRow(_statusPage, "StrengthARow");
            CreateLabel(strengthARow, "通道A强度:");
            _strengthASlider = CreateSlider(strengthARow, 0, 100, 0, OnStrengthAChanged);
            _strengthAInput = CreateInputField(strengthARow, "0", OnStrengthAInputChanged);
            CreateStepButtons(strengthARow, OnStrengthADecrease, OnStrengthAIncrease);

            var strengthBRow = CreateSettingRow(_statusPage, "StrengthBRow");
            CreateLabel(strengthBRow, "通道B强度:");
            _strengthBSlider = CreateSlider(strengthBRow, 0, 100, 0, OnStrengthBChanged);
            _strengthBInput = CreateInputField(strengthBRow, "0", OnStrengthBInputChanged);
            CreateStepButtons(strengthBRow, OnStrengthBDecrease, OnStrengthBIncrease);

            var qrCodeRow = CreateSettingRow(_statusPage, "QRCodeRow");
            CreateLabel(qrCodeRow, "二维码:");
            var qrCodeButtonObj = new GameObject("QRCodeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            qrCodeButtonObj.transform.SetParent(qrCodeRow.transform, false);
            var qrCodeButtonImage = qrCodeButtonObj.GetComponent<Image>();
            qrCodeButtonImage.color = new(0.2f, 0.2f, 0.2f, 1);
            var qrCodeButtonRect = qrCodeButtonObj.GetComponent<RectTransform>();
            qrCodeButtonRect.sizeDelta = new(150, 30);

            var qrCodeButtonText = new GameObject("Text", typeof(Text));
            qrCodeButtonText.transform.SetParent(qrCodeButtonObj.transform, false);
            var qrCodeTextComponent = qrCodeButtonText.GetComponent<Text>();
            qrCodeTextComponent.text = "显示二维码";
            qrCodeTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            qrCodeTextComponent.fontSize = 14;
            qrCodeTextComponent.color = Color.white;
            qrCodeTextComponent.alignment = TextAnchor.MiddleCenter;
            var qrCodeTextRect = qrCodeButtonText.GetComponent<RectTransform>();
            qrCodeTextRect.anchorMin = Vector2.zero;
            qrCodeTextRect.anchorMax = Vector2.one;
            qrCodeTextRect.sizeDelta = Vector2.zero;

            var qrCodeButton = qrCodeButtonObj.GetComponent<Button>();
            qrCodeButton.onClick.AddListener(OnQRCodeButtonClicked);

            BuildQRCodePanel();
        }

        private void BuildQRCodePanel()
        {
            if (_uiRoot == null) return;

            _qrCodePanel = new("QRCodePanel", typeof(Image));
            _qrCodePanel.transform.SetParent(_uiRoot.transform, false);
            var panelImage = _qrCodePanel.GetComponent<Image>();
            panelImage.color = new(0.1f, 0.12f, 0.15f, 0.98f);
            var panelRect = _qrCodePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new(0.5f, 0.5f);
            panelRect.anchorMax = new(0.5f, 0.5f);
            panelRect.pivot = new(0.5f, 0.5f);
            panelRect.sizeDelta = new(400, 450);
            panelRect.anchoredPosition = Vector2.zero;

            var outline = _qrCodePanel.AddComponent<Outline>();
            outline.effectColor = new(0.3f, 0.35f, 0.4f, 0.7f);
            outline.effectDistance = new(2, -2);

            var title = new GameObject("Title", typeof(Text));
            title.transform.SetParent(_qrCodePanel.transform, false);
            var titleText = title.GetComponent<Text>();
            titleText.text = "连接二维码";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new(0, 1);
            titleRect.anchorMax = new(1, 1);
            titleRect.pivot = new(0.5f, 1);
            titleRect.anchoredPosition = new(0, -20);
            titleRect.sizeDelta = new(0, 40);

            var qrCodeImageObj = new GameObject("QRCodeImage", typeof(RectTransform), typeof(RawImage));
            qrCodeImageObj.transform.SetParent(_qrCodePanel.transform, false);
            _qrCodeImage = qrCodeImageObj.GetComponent<RawImage>();
            var qrCodeImageRect = qrCodeImageObj.GetComponent<RectTransform>();
            qrCodeImageRect.anchorMin = new(0.5f, 0.5f);
            qrCodeImageRect.anchorMax = new(0.5f, 0.5f);
            qrCodeImageRect.pivot = new(0.5f, 0.5f);
            qrCodeImageRect.sizeDelta = new(300, 300);
            qrCodeImageRect.anchoredPosition = new(0, -20);

            var closeQRButton = new GameObject("CloseQRButton", typeof(Image), typeof(Button));
            closeQRButton.transform.SetParent(_qrCodePanel.transform, false);
            var closeQRImage = closeQRButton.GetComponent<Image>();
            closeQRImage.color = new(0.2f, 0.2f, 0.2f, 1);
            var closeQRRect = closeQRButton.GetComponent<RectTransform>();
            closeQRRect.anchorMin = new(1, 1);
            closeQRRect.anchorMax = new(1, 1);
            closeQRRect.pivot = new(1, 1);
            closeQRRect.anchoredPosition = new(-10, -10);
            closeQRRect.sizeDelta = new(30, 30);

            var closeQRText = new GameObject("Text", typeof(Text));
            closeQRText.transform.SetParent(closeQRButton.transform, false);
            var closeQRTextComponent = closeQRText.GetComponent<Text>();
            closeQRTextComponent.text = "×";
            closeQRTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            closeQRTextComponent.fontSize = 20;
            closeQRTextComponent.color = Color.white;
            closeQRTextComponent.alignment = TextAnchor.MiddleCenter;
            var closeQRTextRect = closeQRText.GetComponent<RectTransform>();
            closeQRTextRect.anchorMin = Vector2.zero;
            closeQRTextRect.anchorMax = Vector2.one;
            closeQRTextRect.sizeDelta = Vector2.zero;

            var closeQRButtonComponent = closeQRButton.GetComponent<Button>();
            closeQRButtonComponent.onClick.AddListener(() => _qrCodePanel.SetActive(false));

            _qrCodePanel.SetActive(false);
        }

        private void SwitchPage(int pageIndex)
        {
            _currentPage = pageIndex;

            if (_settingsPage != null) _settingsPage.SetActive(pageIndex == 0);
            if (_statusPage != null) _statusPage.SetActive(pageIndex == 1);

            UpdateTabButtonStyle(_settingsTabButton, pageIndex == 0);
            UpdateTabButtonStyle(_statusTabButton, pageIndex == 1);

            if (pageIndex == 1) UpdateStatusPage();
        }

        private void UpdateTabButtonStyle(Button? button, bool isActive)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null) image.color = isActive ? new(0.3f, 0.35f, 0.4f, 1) : new(0.2f, 0.2f, 0.2f, 1);
        }

        private void UpdateStatusPage()
        {
            var controller = ModBehaviour.Instance?.DgLabController;
            if (controller == null)
            {
                if (_connectionStatusText != null) _connectionStatusText.text = "未初始化";
                if (_connectedAppsText != null) _connectedAppsText.text = "0";
                if (_currentStrengthText != null) _currentStrengthText.text = "未获取";
                if (_strengthASlider != null) _strengthASlider.value = 0;
                if (_strengthAInput != null) _strengthAInput.text = "0";
                if (_strengthBSlider != null) _strengthBSlider.value = 0;
                if (_strengthBInput != null) _strengthBInput.text = "0";
                return;
            }

            if (_connectionStatusText != null)
            {
                _connectionStatusText.text = controller.IsInitialized
                    ? controller.HasConnectedApps ? "已连接" : "已初始化，等待连接"
                    : "未初始化";
                _connectionStatusText.color = controller.IsInitialized && controller.HasConnectedApps
                    ? new(0.2f, 0.9f, 0.2f, 1)
                    : Color.white;
            }

            if (_connectedAppsText != null && controller.IsInitialized)
                try
                {
                    _connectedAppsText.text = controller.ConnectedAppsCount.ToString();
                }
                catch
                {
                    _connectedAppsText.text = "未知";
                }

            if (_currentStrengthText != null)
                _currentStrengthText.text = $"通道A: {controller.StrengthA} / 通道B: {controller.StrengthB}";

            if (_strengthASlider != null && _strengthAInput != null)
            {
                _strengthASlider.value = controller.StrengthA;
                _strengthAInput.text = controller.StrengthA.ToString();
            }

            if (_strengthBSlider != null && _strengthBInput != null)
            {
                _strengthBSlider.value = controller.StrengthB;
                _strengthBInput.text = controller.StrengthB.ToString();
            }
        }

        private void OnQRCodeButtonClicked()
        {
            var controller = ModBehaviour.Instance?.DgLabController;
            if (controller == null || string.IsNullOrEmpty(controller.QRCodePath))
            {
                ModLogger.LogWarning("QR code path is not available.");
                return;
            }

            if (!File.Exists(controller.QRCodePath))
            {
                ModLogger.LogWarning($"QR code file not found: {controller.QRCodePath}");
                return;
            }

            try
            {
                var fileData = File.ReadAllBytes(controller.QRCodePath);
                var texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);
                if (_qrCodeImage != null) _qrCodeImage.texture = texture;

                if (_qrCodePanel != null) _qrCodePanel.SetActive(true);
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"Error loading QR code: {ex.Message}");
            }
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

        private static (Button minusButton, Button plusButton) CreateStepButtons(GameObject parent,
            UnityAction onMinusClick, UnityAction onPlusClick, float buttonWidth = 30)
        {
            var minusButtonObj = new GameObject("MinusButton", typeof(RectTransform), typeof(Image), typeof(Button));
            minusButtonObj.transform.SetParent(parent.transform, false);
            var minusButtonImage = minusButtonObj.GetComponent<Image>();
            minusButtonImage.color = new(0.2f, 0.2f, 0.2f, 1);
            var minusButtonRect = minusButtonObj.GetComponent<RectTransform>();
            minusButtonRect.sizeDelta = new(buttonWidth, 25);

            var minusButtonText = new GameObject("Text", typeof(Text));
            minusButtonText.transform.SetParent(minusButtonObj.transform, false);
            var minusTextComponent = minusButtonText.GetComponent<Text>();
            minusTextComponent.text = "-";
            minusTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            minusTextComponent.fontSize = 16;
            minusTextComponent.fontStyle = FontStyle.Bold;
            minusTextComponent.color = Color.white;
            minusTextComponent.alignment = TextAnchor.MiddleCenter;
            var minusTextRect = minusButtonText.GetComponent<RectTransform>();
            minusTextRect.anchorMin = Vector2.zero;
            minusTextRect.anchorMax = Vector2.one;
            minusTextRect.sizeDelta = Vector2.zero;

            var minusButton = minusButtonObj.GetComponent<Button>();
            minusButton.onClick.AddListener(onMinusClick);

            var plusButtonObj = new GameObject("PlusButton", typeof(RectTransform), typeof(Image), typeof(Button));
            plusButtonObj.transform.SetParent(parent.transform, false);
            var plusButtonImage = plusButtonObj.GetComponent<Image>();
            plusButtonImage.color = new(0.2f, 0.2f, 0.2f, 1);
            var plusButtonRect = plusButtonObj.GetComponent<RectTransform>();
            plusButtonRect.sizeDelta = new(buttonWidth, 25);

            var plusButtonText = new GameObject("Text", typeof(Text));
            plusButtonText.transform.SetParent(plusButtonObj.transform, false);
            var plusTextComponent = plusButtonText.GetComponent<Text>();
            plusTextComponent.text = "+";
            plusTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            plusTextComponent.fontSize = 16;
            plusTextComponent.fontStyle = FontStyle.Bold;
            plusTextComponent.color = Color.white;
            plusTextComponent.alignment = TextAnchor.MiddleCenter;
            var plusTextRect = plusButtonText.GetComponent<RectTransform>();
            plusTextRect.anchorMin = Vector2.zero;
            plusTextRect.anchorMax = Vector2.one;
            plusTextRect.sizeDelta = Vector2.zero;

            var plusButton = plusButtonObj.GetComponent<Button>();
            plusButton.onClick.AddListener(onPlusClick);

            return (minusButton, plusButton);
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
            CreateStepButtons(row, OnHurtDurationDecrease, OnHurtDurationIncrease);
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
            CreateStepButtons(row, OnDeathDurationDecrease, OnDeathDurationIncrease);
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

        private void OnHurtDurationDecrease()
        {
            if (_config == null) return;
            var newValue = Mathf.Clamp(_config.HurtDuration - 1, 1, 5);
            if (_hurtDurationSlider != null) _hurtDurationSlider.value = newValue;
        }

        private void OnHurtDurationIncrease()
        {
            if (_config == null) return;
            var newValue = Mathf.Clamp(_config.HurtDuration + 1, 1, 5);
            if (_hurtDurationSlider != null) _hurtDurationSlider.value = newValue;
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

        private void OnDeathDurationDecrease()
        {
            if (_config == null) return;
            var newValue = Mathf.Clamp(_config.DeathDuration - 1, 1, 5);
            if (_deathDurationSlider != null) _deathDurationSlider.value = newValue;
        }

        private void OnDeathDurationIncrease()
        {
            if (_config == null) return;
            var newValue = Mathf.Clamp(_config.DeathDuration + 1, 1, 5);
            if (_deathDurationSlider != null) _deathDurationSlider.value = newValue;
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

        private void SaveConfig()
        {
            if (_config == null) return;
            ConfigManager.SaveConfigToFile(_config, "DGLabConfig.json");
        }

        private void OnStrengthAChanged(float value)
        {
            var intValue = (int)value;
            if (_strengthAInput != null) _strengthAInput.text = intValue.ToString();
            SetStrengthAsync(Channel.A, intValue);
        }

        private void OnStrengthAInputChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (int.TryParse(value, out var intValue))
            {
                intValue = Mathf.Clamp(intValue, 0, 100);
                if (_strengthASlider != null) _strengthASlider.value = intValue;
                if (_strengthAInput != null) _strengthAInput.text = intValue.ToString();
                SetStrengthAsync(Channel.A, intValue);
            }
        }

        private void OnStrengthBChanged(float value)
        {
            var intValue = (int)value;
            if (_strengthBInput != null) _strengthBInput.text = intValue.ToString();
            SetStrengthAsync(Channel.B, intValue);
        }

        private void OnStrengthBInputChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (int.TryParse(value, out var intValue))
            {
                intValue = Mathf.Clamp(intValue, 0, 100);
                if (_strengthBSlider != null) _strengthBSlider.value = intValue;
                if (_strengthBInput != null) _strengthBInput.text = intValue.ToString();
                SetStrengthAsync(Channel.B, intValue);
            }
        }

        private void OnStrengthADecrease()
        {
            if (_strengthASlider == null) return;
            var currentValue = (int)_strengthASlider.value;
            var newValue = Mathf.Clamp(currentValue - 1, 0, 100);
            _strengthASlider.value = newValue;
        }

        private void OnStrengthAIncrease()
        {
            if (_strengthASlider == null) return;
            var currentValue = (int)_strengthASlider.value;
            var newValue = Mathf.Clamp(currentValue + 1, 0, 100);
            _strengthASlider.value = newValue;
        }

        private void OnStrengthBDecrease()
        {
            if (_strengthBSlider == null) return;
            var currentValue = (int)_strengthBSlider.value;
            var newValue = Mathf.Clamp(currentValue - 1, 0, 100);
            _strengthBSlider.value = newValue;
        }

        private void OnStrengthBIncrease()
        {
            if (_strengthBSlider == null) return;
            var currentValue = (int)_strengthBSlider.value;
            var newValue = Mathf.Clamp(currentValue + 1, 0, 100);
            _strengthBSlider.value = newValue;
        }

        private void SetStrengthAsync(Channel channel, int strength)
        {
            var controller = ModBehaviour.Instance?.DgLabController;
            if (controller == null) return;
            _ = controller.SetStrengthAsync(channel, strength);
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
            StartCoroutine(UpdateStatusPagePeriodically());
            if (_currentPage == 1) UpdateStatusPage();
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

        private IEnumerator UpdateStatusPagePeriodically()
        {
            while (_uiActive)
            {
                if (_currentPage == 1) UpdateStatusPage();
                yield return new WaitForSeconds(1f);
            }
        }
    }
}