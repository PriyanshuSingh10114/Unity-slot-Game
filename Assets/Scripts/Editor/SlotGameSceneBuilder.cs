#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SlotGame.Audio;
using SlotGame.Core;
using SlotGame.Payouts;
using SlotGame.Reels;
using SlotGame.UI;

namespace SlotGame.Editor
{
    /// <summary>
    /// Editor automation tool that programmatically constructs the complete slot machine scene,
    /// canvas hierarchies, reel containers, masking viewports, and serialized component bindings.
    /// Menu: Tools -> Slot Game -> Rebuild Main Scene
    /// </summary>
    public static class SlotGameSceneBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/MainScene.unity";
        private const string CONFIG_PATH = "Assets/ScriptableObjects/DefaultSlotGameConfig.asset";

        [MenuItem("Tools/Slot Game/Rebuild Main Scene")]
        public static void BuildMainScene()
        {
            // 1. Ensure ScriptableObjects exist
            var config = EnsureGameConfigAndSymbols();

            // 2. Create and setup new 2D Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3. Camera
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.05f, 0.12f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0f, 0f, -10f);

            // 4. Event System
            var eventObj = new GameObject("EventSystem");
            eventObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 5. Canvas
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // 6. Audio Manager Object
            var audioObj = new GameObject("AudioManager");
            var audioSourceSfx = audioObj.AddComponent<AudioSource>();
            audioSourceSfx.playOnAwake = false;
            var audioSourceLoop = audioObj.AddComponent<AudioSource>();
            audioSourceLoop.playOnAwake = false;
            var audioManager = audioObj.AddComponent<SlotAudioManager>();

            // Wire audio clips
            var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/button_click.wav");
            var loopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/reel_spin_loop.wav");
            var stopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/reel_stop.wav");
            var winClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/win_chime.wav");
            var lossClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/loss_sound.wav");
            var jackpotClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/jackpot_fanfare.wav");
            audioManager.Initialize(audioSourceSfx, audioSourceLoop, clickClip, loopClip, stopClip, winClip, lossClip, jackpotClip);

            // 7. Background Image
            var bgObj = CreateUIElement("Background", canvasObj.transform);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/bg_gradient.png");
            StretchToParent(bgObj.GetComponent<RectTransform>());

            // 8. Slot Machine Cabinet Hierarchy
            var cabinetObj = CreateUIElement("CabinetRoot", canvasObj.transform);
            var cabinetRt = cabinetObj.GetComponent<RectTransform>();
            cabinetRt.anchoredPosition = new Vector2(0f, 50f);
            cabinetRt.sizeDelta = new Vector2(816f, 624f);

            // Cabinet Base
            var cabinetBaseObj = CreateUIElement("CabinetBase", cabinetObj.transform);
            var cabinetBaseImg = cabinetBaseObj.AddComponent<Image>();
            cabinetBaseImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Frames/slot-machine1.png");
            StretchToParent(cabinetBaseObj.GetComponent<RectTransform>());

            // Middle Box Frame
            var middleBoxObj = CreateUIElement("MiddleBox", cabinetObj.transform);
            var middleBoxRt = middleBoxObj.GetComponent<RectTransform>();
            middleBoxRt.anchoredPosition = new Vector2(0f, -22f);
            middleBoxRt.sizeDelta = new Vector2(658f, 277f);
            var middleBoxImg = middleBoxObj.AddComponent<Image>();
            middleBoxImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Frames/slot_machine_Middle_box.png");

            // Reel Viewport (Masked area)
            var viewportObj = CreateUIElement("ReelViewport", middleBoxObj.transform);
            var viewportRt = viewportObj.GetComponent<RectTransform>();
            viewportRt.anchoredPosition = Vector2.zero;
            viewportRt.sizeDelta = new Vector2(530f, 210f);
            var viewportMask = viewportObj.AddComponent<RectMask2D>();

            // Create 3 Reels
            float[] reelXPositions = new float[] { -170f, 0f, 170f };
            var reelControllers = new SlotReelController[3];

            for (int r = 0; r < 3; r++)
            {
                var reelObj = CreateUIElement($"Reel_{r}", viewportObj.transform);
                var reelRt = reelObj.GetComponent<RectTransform>();
                reelRt.anchoredPosition = new Vector2(reelXPositions[r], 0f);
                reelRt.sizeDelta = new Vector2(160f, 210f);

                var controller = reelObj.AddComponent<SlotReelController>();
                reelControllers[r] = controller;

                // Create 5 symbol views per reel for seamless recycling
                var symbolViews = new Image[5];
                for (int v = 0; v < 5; v++)
                {
                    var viewObj = CreateUIElement($"SymbolView_{v}", reelObj.transform);
                    var viewRt = viewObj.GetComponent<RectTransform>();
                    viewRt.sizeDelta = new Vector2(110f, 110f);
                    viewRt.anchoredPosition = new Vector2(0f, (v - 2) * 130f);
                    var img = viewObj.AddComponent<Image>();
                    img.preserveAspect = true;
                    symbolViews[v] = img;
                }

                // Use reflection or serialized object to assign private serialized fields
                var serializedReel = new SerializedObject(controller);
                serializedReel.FindProperty("_reelIndex").intValue = r;
                serializedReel.FindProperty("_reelStripContainer").objectReferenceValue = reelRt;
                serializedReel.FindProperty("_symbolHeight").floatValue = 130f;
                serializedReel.FindProperty("_spinSpeed").floatValue = 1600f;

                var viewsProp = serializedReel.FindProperty("_symbolViews");
                viewsProp.arraySize = symbolViews.Length;
                for (int v = 0; v < symbolViews.Length; v++)
                {
                    viewsProp.GetArrayElementAtIndex(v).objectReferenceValue = symbolViews[v];
                }
                serializedReel.ApplyModifiedProperties();
            }

            // Cabinet Foreground Frame Overlay
            var frameOverlayObj = CreateUIElement("CabinetFrameOverlay", cabinetObj.transform);
            var frameOverlayImg = frameOverlayObj.AddComponent<Image>();
            frameOverlayImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Frames/slot-machine4.png");
            frameOverlayImg.raycastTarget = false;
            StretchToParent(frameOverlayObj.GetComponent<RectTransform>());

            // 9. UI Dashboard Panel
            var dashboardObj = CreateUIElement("DashboardPanel", canvasObj.transform);
            var dashboardRt = dashboardObj.GetComponent<RectTransform>();
            dashboardRt.anchorMin = new Vector2(0.5f, 0f);
            dashboardRt.anchorMax = new Vector2(0.5f, 0f);
            dashboardRt.pivot = new Vector2(0.5f, 0f);
            dashboardRt.anchoredPosition = new Vector2(0f, 30f);
            dashboardRt.sizeDelta = new Vector2(1200f, 220f);

            // Status Message Text
            var msgObj = CreateTextElement("StatusMessageText", dashboardObj.transform, "PRESS SPIN TO PLAY!", 28, TextAnchor.MiddleCenter, Color.yellow);
            var msgRt = msgObj.GetComponent<RectTransform>();
            msgRt.anchoredPosition = new Vector2(0f, 175f);
            msgRt.sizeDelta = new Vector2(900f, 40f);

            // Balance Display
            var balObj = CreateTextElement("BalanceText", dashboardObj.transform, "CREDITS: 1,000", 30, TextAnchor.MiddleLeft, Color.white);
            var balRt = balObj.GetComponent<RectTransform>();
            balRt.anchoredPosition = new Vector2(-420f, 60f);
            balRt.sizeDelta = new Vector2(300f, 45f);

            // Win Display
            var winObj = CreateTextElement("WinAmountText", dashboardObj.transform, "WIN: 0", 30, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.2f));
            var winRt = winObj.GetComponent<RectTransform>();
            winRt.anchoredPosition = new Vector2(-420f, 10f);
            winRt.sizeDelta = new Vector2(300f, 45f);

            // Bet Controls
            var betMinusBtnObj = CreateButtonElement("BetMinusButton", dashboardObj.transform, "-", 36);
            var betMinusRt = betMinusBtnObj.GetComponent<RectTransform>();
            betMinusRt.anchoredPosition = new Vector2(-160f, 35f);
            betMinusRt.sizeDelta = new Vector2(55f, 55f);

            var betTextObj = CreateTextElement("BetText", dashboardObj.transform, "BET: 10", 28, TextAnchor.MiddleCenter, Color.white);
            var betTextRt = betTextObj.GetComponent<RectTransform>();
            betTextRt.anchoredPosition = new Vector2(-70f, 35f);
            betTextRt.sizeDelta = new Vector2(110f, 45f);

            var betPlusBtnObj = CreateButtonElement("BetPlusButton", dashboardObj.transform, "+", 36);
            var betPlusRt = betPlusBtnObj.GetComponent<RectTransform>();
            betPlusRt.anchoredPosition = new Vector2(20f, 35f);
            betPlusRt.sizeDelta = new Vector2(55f, 55f);

            // Spin Button (Main round button)
            var spinBtnObj = CreateButtonElement("SpinButton", dashboardObj.transform, "SPIN", 34);
            var spinBtnRt = spinBtnObj.GetComponent<RectTransform>();
            spinBtnRt.anchoredPosition = new Vector2(160f, 35f);
            spinBtnRt.sizeDelta = new Vector2(130f, 130f);

            var spinBtn = spinBtnObj.GetComponent<Button>();
            var spinNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Buttons/btn_spin_normal.png");
            var spinHoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Buttons/btn_spin_highlighted.png");
            var spinPressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Buttons/btn_spin_pressed.png");
            var spinDisabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Buttons/btn_spin_disabled.png");

            if (spinNormalSprite != null)
            {
                spinBtnObj.GetComponent<Image>().sprite = spinNormalSprite;
                spinBtn.transition = Selectable.Transition.SpriteSwap;
                var ss = spinBtn.spriteState;
                ss.highlightedSprite = spinHoverSprite;
                ss.pressedSprite = spinPressedSprite;
                ss.disabledSprite = spinDisabledSprite;
                spinBtn.spriteState = ss;
            }

            // Auto Spin Button
            var autoBtnObj = CreateButtonElement("AutoSpinButton", dashboardObj.transform, "AUTO", 20);
            var autoBtnRt = autoBtnObj.GetComponent<RectTransform>();
            autoBtnRt.anchoredPosition = new Vector2(300f, 35f);
            autoBtnRt.sizeDelta = new Vector2(100f, 55f);

            // Paytable & Sound Buttons
            var paytableBtnObj = CreateButtonElement("PaytableButton", dashboardObj.transform, "PAYTABLE", 18);
            var paytableRt = paytableBtnObj.GetComponent<RectTransform>();
            paytableRt.anchoredPosition = new Vector2(430f, 65f);
            paytableRt.sizeDelta = new Vector2(120f, 45f);

            var muteBtnObj = CreateButtonElement("MuteButton", dashboardObj.transform, "SOUND: ON", 18);
            var muteRt = muteBtnObj.GetComponent<RectTransform>();
            muteRt.anchoredPosition = new Vector2(430f, 10f);
            muteRt.sizeDelta = new Vector2(120f, 45f);

            // 10. Win Banner Overlay
            var winBannerObj = CreateUIElement("WinBannerOverlay", canvasObj.transform);
            var winBannerRt = winBannerObj.GetComponent<RectTransform>();
            winBannerRt.anchoredPosition = new Vector2(0f, 60f);
            winBannerRt.sizeDelta = new Vector2(700f, 160f);
            var bannerImg = winBannerObj.AddComponent<Image>();
            bannerImg.color = new Color(0.1f, 0.05f, 0.2f, 0.9f);
            var bannerTextObj = CreateTextElement("BannerText", winBannerObj.transform, "WINNER!", 42, TextAnchor.MiddleCenter, Color.yellow);
            StretchToParent(bannerTextObj.GetComponent<RectTransform>());

            // 11. Win Popup Modal Dialog
            var popupModalObj = CreateUIElement("WinPopupModal", canvasObj.transform);
            var popupRt = popupModalObj.GetComponent<RectTransform>();
            popupRt.anchoredPosition = Vector2.zero;
            popupRt.sizeDelta = new Vector2(720f, 360f);
            var popupImg = popupModalObj.AddComponent<Image>();
            popupImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Frames/popup.png");

            var popupTitleObj = CreateTextElement("PopupTitleText", popupModalObj.transform, "MEGA JACKPOT!", 36, TextAnchor.MiddleCenter, Color.yellow);
            var pTitleRt = popupTitleObj.GetComponent<RectTransform>();
            pTitleRt.anchoredPosition = new Vector2(0f, 60f);
            pTitleRt.sizeDelta = new Vector2(500f, 50f);

            var popupAmountObj = CreateTextElement("PopupAmountText", popupModalObj.transform, "+1,000 CREDITS", 32, TextAnchor.MiddleCenter, Color.white);
            var pAmtRt = popupAmountObj.GetComponent<RectTransform>();
            pAmtRt.anchoredPosition = new Vector2(0f, 0f);
            pAmtRt.sizeDelta = new Vector2(500f, 50f);

            var popupOkBtnObj = CreateButtonElement("PopupOkButton", popupModalObj.transform, "CLAIM", 24);
            var pOkRt = popupOkBtnObj.GetComponent<RectTransform>();
            pOkRt.anchoredPosition = new Vector2(0f, -80f);
            pOkRt.sizeDelta = new Vector2(180f, 55f);

            // 12. Paytable Modal Dialog
            var paytableModalObj = CreateUIElement("PaytableModal", canvasObj.transform);
            var paytableModalRt = paytableModalObj.GetComponent<RectTransform>();
            paytableModalRt.anchoredPosition = Vector2.zero;
            paytableModalRt.sizeDelta = new Vector2(650f, 450f);
            var ptModalImg = paytableModalObj.AddComponent<Image>();
            ptModalImg.color = new Color(0.12f, 0.08f, 0.2f, 0.96f);

            string paytableContent = "=== PAYTABLE & RULES ===\n\n" +
                                     "SEVEN (777)   :  25x Multiplier (JACKPOT)\n" +
                                     "BAR (BARx3)   :  10x Multiplier\n" +
                                     "BELL (BELLx3) :   5x Multiplier\n" +
                                     "CHERRY (🍒x3) :   3x Multiplier\n\n" +
                                     "Rules:\n" +
                                     "• Win when all 3 reels show the exact same symbol.\n" +
                                     "• Payout = Current Bet × Multiplier.\n" +
                                     "• Simulated credits only. Play responsibly!";
            var ptTextObj = CreateTextElement("PaytableText", paytableModalObj.transform, paytableContent, 22, TextAnchor.MiddleCenter, Color.white);
            var ptTextRt = ptTextObj.GetComponent<RectTransform>();
            ptTextRt.anchoredPosition = new Vector2(0f, 30f);
            ptTextRt.sizeDelta = new Vector2(580f, 320f);

            var ptCloseBtnObj = CreateButtonElement("PaytableCloseButton", paytableModalObj.transform, "CLOSE", 22);
            var ptCloseRt = ptCloseBtnObj.GetComponent<RectTransform>();
            ptCloseRt.anchoredPosition = new Vector2(0f, -170f);
            ptCloseRt.sizeDelta = new Vector2(160f, 50f);

            // 13. UI Controller Component & Serialized Wiring
            var uiController = canvasObj.AddComponent<SlotUIController>();
            var serializedUi = new SerializedObject(uiController);
            serializedUi.FindProperty("_balanceText").objectReferenceValue = balObj.GetComponent<Text>();
            serializedUi.FindProperty("_betText").objectReferenceValue = betTextObj.GetComponent<Text>();
            serializedUi.FindProperty("_winAmountText").objectReferenceValue = winObj.GetComponent<Text>();
            serializedUi.FindProperty("_messageText").objectReferenceValue = msgObj.GetComponent<Text>();

            serializedUi.FindProperty("_spinButton").objectReferenceValue = spinBtn;
            serializedUi.FindProperty("_betMinusButton").objectReferenceValue = betMinusBtnObj.GetComponent<Button>();
            serializedUi.FindProperty("_betPlusButton").objectReferenceValue = betPlusBtnObj.GetComponent<Button>();
            serializedUi.FindProperty("_autoSpinButton").objectReferenceValue = autoBtnObj.GetComponent<Button>();
            serializedUi.FindProperty("_autoSpinButtonText").objectReferenceValue = autoBtnObj.GetComponentInChildren<Text>();
            serializedUi.FindProperty("_paytableButton").objectReferenceValue = paytableBtnObj.GetComponent<Button>();
            serializedUi.FindProperty("_muteButton").objectReferenceValue = muteBtnObj.GetComponent<Button>();
            serializedUi.FindProperty("_muteButtonText").objectReferenceValue = muteBtnObj.GetComponentInChildren<Text>();

            serializedUi.FindProperty("_winPopupRoot").objectReferenceValue = popupModalObj;
            serializedUi.FindProperty("_winPopupTitleText").objectReferenceValue = popupTitleObj.GetComponent<Text>();
            serializedUi.FindProperty("_winPopupAmountText").objectReferenceValue = popupAmountObj.GetComponent<Text>();
            serializedUi.FindProperty("_winPopupCloseButton").objectReferenceValue = popupOkBtnObj.GetComponent<Button>();
            serializedUi.FindProperty("_winPopupOkButton").objectReferenceValue = popupOkBtnObj.GetComponent<Button>();

            serializedUi.FindProperty("_paytableModalRoot").objectReferenceValue = paytableModalObj;
            serializedUi.FindProperty("_paytableCloseButton").objectReferenceValue = ptCloseBtnObj.GetComponent<Button>();

            serializedUi.FindProperty("_winBannerRoot").objectReferenceValue = winBannerObj;
            serializedUi.FindProperty("_winBannerText").objectReferenceValue = bannerTextObj.GetComponent<Text>();
            serializedUi.ApplyModifiedProperties();

            // 14. Game Manager Object
            var gmObj = new GameObject("SlotGameManager");
            var gameManager = gmObj.AddComponent<SlotGameManager>();
            var serializedGm = new SerializedObject(gameManager);
            serializedGm.FindProperty("_config").objectReferenceValue = config;
            serializedGm.FindProperty("_ui").objectReferenceValue = uiController;
            serializedGm.FindProperty("_audio").objectReferenceValue = audioManager;

            var reelsProp = serializedGm.FindProperty("_reels");
            reelsProp.arraySize = reelControllers.Length;
            for (int r = 0; r < reelControllers.Length; r++)
            {
                reelsProp.GetArrayElementAtIndex(r).objectReferenceValue = reelControllers[r];
            }
            serializedGm.ApplyModifiedProperties();

            // 15. Save Scene
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log($"[SlotGameSceneBuilder] MainScene successfully built and saved to {SCENE_PATH}");
        }

        private static SlotGameConfig EnsureGameConfigAndSymbols()
        {
            Directory.CreateDirectory("Assets/ScriptableObjects");

            // 1. Symbol Seven
            var symSeven = AssetDatabase.LoadAssetAtPath<SlotSymbolData>("Assets/ScriptableObjects/Symbol_Seven.asset");
            if (symSeven == null)
            {
                symSeven = ScriptableObject.CreateInstance<SlotSymbolData>();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Symbols/symbol_seven.png");
                symSeven.Initialize(0, "Seven", 25, sprite, 1);
                AssetDatabase.CreateAsset(symSeven, "Assets/ScriptableObjects/Symbol_Seven.asset");
            }

            // 2. Symbol Cherry
            var symCherry = AssetDatabase.LoadAssetAtPath<SlotSymbolData>("Assets/ScriptableObjects/Symbol_Cherry.asset");
            if (symCherry == null)
            {
                symCherry = ScriptableObject.CreateInstance<SlotSymbolData>();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Symbols/symbol_cherry.png");
                symCherry.Initialize(1, "Cherry", 3, sprite, 1);
                AssetDatabase.CreateAsset(symCherry, "Assets/ScriptableObjects/Symbol_Cherry.asset");
            }

            // 3. Symbol Bell
            var symBell = AssetDatabase.LoadAssetAtPath<SlotSymbolData>("Assets/ScriptableObjects/Symbol_Bell.asset");
            if (symBell == null)
            {
                symBell = ScriptableObject.CreateInstance<SlotSymbolData>();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Symbols/symbol_bell.png");
                symBell.Initialize(2, "Bell", 5, sprite, 1);
                AssetDatabase.CreateAsset(symBell, "Assets/ScriptableObjects/Symbol_Bell.asset");
            }

            // 4. Symbol BAR
            var symBar = AssetDatabase.LoadAssetAtPath<SlotSymbolData>("Assets/ScriptableObjects/Symbol_Bar.asset");
            if (symBar == null)
            {
                symBar = ScriptableObject.CreateInstance<SlotSymbolData>();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Symbols/symbol_bar.png");
                symBar.Initialize(3, "Bar", 10, sprite, 1);
                AssetDatabase.CreateAsset(symBar, "Assets/ScriptableObjects/Symbol_Bar.asset");
            }

            // Game Config
            var config = AssetDatabase.LoadAssetAtPath<SlotGameConfig>(CONFIG_PATH);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<SlotGameConfig>();
                var so = new SerializedObject(config);
                so.FindProperty("_startingBalance").intValue = 1000;
                so.FindProperty("_defaultBetIndex").intValue = 0;
                var symsProp = so.FindProperty("_symbols");
                symsProp.arraySize = 4;
                symsProp.GetArrayElementAtIndex(0).objectReferenceValue = symSeven;
                symsProp.GetArrayElementAtIndex(1).objectReferenceValue = symCherry;
                symsProp.GetArrayElementAtIndex(2).objectReferenceValue = symBell;
                symsProp.GetArrayElementAtIndex(3).objectReferenceValue = symBar;
                so.ApplyModifiedProperties();
                AssetDatabase.CreateAsset(config, CONFIG_PATH);
            }

            AssetDatabase.SaveAssets();
            return config;
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static GameObject CreateTextElement(string name, Transform parent, string text, int fontSize, TextAnchor alignment, Color color)
        {
            var go = CreateUIElement(name, parent);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color;
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return go;
        }

        private static GameObject CreateButtonElement(string name, Transform parent, string label, int fontSize)
        {
            var go = CreateUIElement(name, parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.15f, 0.35f, 0.95f);
            var btn = go.AddComponent<Button>();

            var textGo = CreateTextElement("Text", go.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            StretchToParent(textGo.GetComponent<RectTransform>());

            return go;
        }

        private static void StretchToParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
