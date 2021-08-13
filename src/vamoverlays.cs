// #define DEBUG
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MeshVR;
using Request = MeshVR.AssetLoader.AssetBundleFromFileRequest;

// VAMOverlays
// Overlays system to display fades, titles and subtitles

namespace VAMOverlaysPlugin
{
	public class VAMOverlays : MVRScript
	{
		private static readonly List<string> SubtitlesQuotes = new List<string>
		{
			// ReSharper disable StringLiteralTypo
			"<b>Ripley:</b> You better just start dealing with it, Hudson! Listen to me!\nHudson, just deal with it, because we need you and I'm sick of your bullshit.",
			"<b>Apone:</b> All right, sweethearts, what are you waiting for? Breakfast in bed?\nIt's another glorious day in the Corps.",
			"<b>Hudson:</b> We're on the express elevator to hell, going down!",
			"<b>Vasquez:</b> You always were an asshole, Gorman.",
			"<b>Drake:</b> They ain't pay us enough for this, man.\n<b>Dietrich:</b> Not enough to wake up every day to your face, Drake.",
			"<b>Vasquez:</b> How many combat drops?\n<b>Gorman:</b> Uh, two. Including this one.",
			"<b>Hicks:</b> <i>**pulls out a shotgun**</i> I like to keep this handy. For close encounters.\n<b>Frost:</b> Yeah, I heard that."
			// ReSharper restore StringLiteralTypo
		};

		private static readonly Dictionary<string, string> FontList = new Dictionary<string, string>
		{
			// ReSharper disable StringLiteralTypo
			{ "Arial", null },
			{ "Amalia", "Assets/VAMFonts/amalia.ttf" },
			{ "AnimeAceBB", "Assets/VAMFonts/animeacebb_reg.ttf" },
			{ "AsteraV2", "Assets/VAMFonts/astera_v2.ttf" },
			{ "BebasRegular", "Assets/VAMFonts/bebas_regular.ttf" },
			{ "BirdsOfParadise", "Assets/VAMFonts/birds_of_paradise.ttf" },
			{ "GlitchInside", "Assets/VAMFonts/glitch_inside.otf" },
			{ "GosmickSans", "Assets/VAMFonts/gosmick_sans.ttf" },
			{ "HomerSimpsonRevised", "Assets/VAMFonts/homer_simpson_revised.ttf" },
			{ "KionaRegular", "Assets/VAMFonts/kiona_regular.ttf" },
			{ "OptimusPrinceps", "Assets/VAMFonts/optimus_princeps.ttf" },
			{ "Oswald", "Assets/VAMFonts/oswald.ttf" },
			{ "PlanetKosmos", "Assets/VAMFonts/planet_kosmos.ttf" },
			{ "Poppins", "Assets/VAMFonts/poppins.ttf" },
			{ "SpaceAge", "Assets/VAMFonts/space_age.ttf" }
			// ReSharper restore StringLiteralTypo
		};

		private string _fontsBundlePath;

		private readonly List<string> _fontChoices = FontList.Select(kvp => kvp.Key).ToList();
		private Dictionary<string, Font> _fontAssets;

		// Game Objects and components
		private GameObject _vamOverlaysGO;
		private Canvas _overlaysCanvas;

		private GameObject _fadeImgGO;
		private Image _fadeImg;

		private GameObject _subtitlesTxtGO;
		private Text _subtitlesTxt;
		private Shadow _subtitlesTxtShadow;
		private RectTransform _subtitlesRecTr;

		private JSONStorableBool _fadeAtStart;
		private JSONStorableBool _fadeInOnLoadComplete;
		private JSONStorableColor _fadeColor;
		private JSONStorableFloat _fadeInTime;
		private JSONStorableFloat _fadeOutTime;

		private JSONStorableColor _subtitlesColor;
		private JSONStorableString _subtitlesText;
		private JSONStorableFloat _subtitlesSize;
		private JSONStorableStringChooser _subtitlesFontChoice;
		private JSONStorableStringChooser _subtitleAlignmentChoice;
		private JSONStorableFloat _subtitlesFadeDuration;
		private JSONStorableFloat _subtitlesShowDuration;

		private JSONStorableAction _triggerFadeIn;
		private JSONStorableAction _triggerFadeOut;
		private JSONStorableAction _triggerFadeInInstant;
		private JSONStorableAction _triggerFadeOutInstant;
		private JSONStorableColor _setFadeColor;
		private JSONStorableFloat _setFadeInTime;
		private JSONStorableFloat _setFadeOutTime;
		private JSONStorableColor _setSubtitlesColor;
		private JSONStorableFloat _setSubtitlesSize;
		private JSONStorableStringChooser _setSubtitlesFont;
		private JSONStorableStringChooser _setSubtitlesAlignment;
		private JSONStorableFloat _setSubtitlesFadeDuration;
		private JSONStorableFloat _setSubtitlesShowDuration;

		private JSONStorableAction _showSubtitles5Secs;
		private JSONStorableAction _showSubtitlesPermanent;
		private JSONStorableString _setSubtitlesTextAndShowNow;
		private JSONStorableAction _hideSubtitles;

#if(DEBUG)
		private JSONStorableString _debugArea;
#endif

		public override void Init()
		{
			try
			{
				if (containingAtom.type != "Empty")
				{
					SuperController.LogError("Please add VAMOverlays on an Empty Atom");
					return;
				}

				_fontsBundlePath = $"{GetPluginPath(this)}/assets/fonts.fontbundle";

				// Loading fonts
				_fontAssets = new Dictionary<string, Font>
				{
					// I'm creating Arial by default because this font rocks for subtitles, and if the bundle fails, we have at least one font
					["Arial"] = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf")
				};
				var request = new AssetLoader.AssetBundleFromFileRequest { path = _fontsBundlePath, callback = OnFontsBundleLoaded };
				AssetLoader.QueueLoadAssetBundleFromFile(request);

				// **************************************************
				// ** LEFT : Fade properties
				// **************************************************

				// Debugging
#if(DEBUG)
				_debugArea = new JSONStorableString("Debug", "");
				var debugAreaField = CreateTextField(_debugArea, false);
				debugAreaField.height = 150.0f;
#endif

				// Title fading
				var fadeTitle = new JSONStorableString(
					"Help",
					"<color=#000><size=30><b>Fading settings</b></size></color>\nChange the appearance and behavior of the default fade"
				);
				var fadeTitleField = CreateTextField(fadeTitle, false);
				fadeTitleField.height = 50.0f;

				// Color picker
				var hsvcFa = HSVColorPicker.RGBToHSV(0f, 0f, 0f);
				_fadeColor = new JSONStorableColor("Fade Color", hsvcFa, FadeColorCallback);
				CreateColorPicker(_fadeColor);

				// Fade at start enabled ?
				_fadeAtStart = new JSONStorableBool("Fade at start", false, FadeAtStartCallback)
				{
					storeType = JSONStorableParam.StoreType.Full
				};
				CreateToggle(_fadeAtStart, false);

				_fadeInOnLoadComplete = new JSONStorableBool("Fade in on load complete", false, FadeInOnLoadCompleteCallback)
				{
					storeType = JSONStorableParam.StoreType.Full
				};
				CreateToggle(_fadeInOnLoadComplete, false);

				// Fade in time
				_fadeInTime = new JSONStorableFloat("Fade in time", 5.0f, val => _setFadeInTime.valNoCallback = val, 0f, 120.0f, true, true)
				{
					storeType = JSONStorableParam.StoreType.Full
				};
				CreateSlider(_fadeInTime, false);

				// Fade out time
				_fadeOutTime = new JSONStorableFloat("Fade out time", 5.0f, val => _setFadeOutTime.valNoCallback = val, 0f, 120.0f, true, true)
				{
					storeType = JSONStorableParam.StoreType.Full
				};
				CreateSlider(_fadeOutTime, false);

				// Test Fade in
				var fadeInTestButton = CreateButton("Test Fade In");
				if (fadeInTestButton != null)
				{
					fadeInTestButton.button.onClick.AddListener(TestFadeIn);
				}

				// Test fade out
				var fadeOutTestButton = CreateButton("Test Fade Out");
				if (fadeOutTestButton != null)
				{
					fadeOutTestButton.button.onClick.AddListener(TestFadeOut);
				}

				// *****************************
				// HELP
				// *****************************
				var fadeHelpText = new JSONStorableString("Help",
					"<color=#000><size=35><b>Fading help</b></size></color>\n\n" +
					"<color=#333>" +
					"<b>Fade color :</b> The color of the fade effect\n\n" +
					"<b>Fade at start :</b> If the fade should cover the screen when the scene has finished loading\nIf you don't control the fade in afterwards, the scene will stay black (or the color you have selected) and the player will only be able to see the menus.\n\n" +
					"<b>Fade in on load complete :</b> Whether to automatically fade in as soon as the scene finishes loading.\n\n" +
					"<b>Fade in time :</b> How much time it takes to fade from the color being opaque to completely transparent.\n\n" +
					"<b>Fade out time :</b> How much time it takes to fade from the color being completely transparent to opaque.\n\n" +
					"</color>"
				);
				var fadeHelpTextField = CreateTextField(fadeHelpText, false);
				fadeHelpTextField.height = 800.0f;

				// **************************************************
				// ** RIGHT : Subtitles properties
				// **************************************************
				_subtitlesText = new JSONStorableString("Set subtitles text", "", SubtitlesValueCallback) { isStorable = false };

				// Title subtitles
				var subtitlesTitle = new JSONStorableString(
					"Help",
					"<color=#000><size=30><b>Subtitles settings</b></size></color>\nChange the appearance and behavior of the default subtitles"
				);
				var subtitlesTitleField = CreateTextField(subtitlesTitle, true);
				subtitlesTitleField.height = 20.0f;

				// Color picker
				var hsvcSt = HSVColorPicker.RGBToHSV(1f, 1f, 1f);
				_subtitlesColor = new JSONStorableColor("Subtitles Color", hsvcSt, SubtitlesColorCallback);
				CreateColorPicker(_subtitlesColor, true);

				// Preview
				var previewSubtitles = new JSONStorableBool("Preview subtitles", false, PreviewSubtitlesCallback);
				CreateToggle(previewSubtitles, true);

				_subtitlesSize = new JSONStorableFloat("Subtitles size", 18, SubtitlesSizeCallback, 12, 100);
				var subtitlesSizeSlider = CreateSlider(_subtitlesSize, true);
				subtitlesSizeSlider.valueFormat = "F1";

				_subtitlesFontChoice = new JSONStorableStringChooser(
					"Font",
					_fontChoices,
					_fontChoices[0],
					"Font",
					ChangeSubtitlesFont
				);
				var sfcUdp = CreateScrollablePopup(_subtitlesFontChoice, true);
				sfcUdp.labelWidth = 150f;

				_subtitleAlignmentChoice = new JSONStorableStringChooser(
					"Text alignment",
					new List<string>
					{
						"Bottom",
						"Center",
						"Top"
					},
					"Bottom",
					"Text alignment",
					SubtitlesAlignmentCallback
				);
				var sacUdp = CreateScrollablePopup(_subtitleAlignmentChoice, true);
				sacUdp.labelWidth = 300f;

				_subtitlesFadeDuration = new JSONStorableFloat("Subtitles fade duration", 1f, 0f, 10f, false);
				CreateSlider(_subtitlesFadeDuration, true);

				_subtitlesShowDuration = new JSONStorableFloat("Subtitles duration", 5f, 0f, 30f, false);
				CreateSlider(_subtitlesShowDuration, true);

				// *****************************
				// HELP
				// *****************************
				var subtitlesHelpText = new JSONStorableString("Help",
					"<color=#000><size=35><b>Subtitles help</b></size></color>\n\n" +
					"<color=#333>" +
					"<b>Subtitles size :</b> The size of the text (will be larger in VR).\n\n" +
					"<b>Font :</b> The font family used by the subtitles.\n\n" +
					"<b>Text alignment :</b> Where will the subtitles show on the screen.\n\n" +
					"<b>Subtitles fade duration :</b> How much time it takes to fade the subtitles in or out.\n\n" +
					"<b>Subtitles duration :</b> When triggered using 'Set subtitles text and show now', determines after how many seconds to hide the subtitles.\n\n" +
					"</color>"
				);
				var subtitlesHelpTextField = CreateTextField(subtitlesHelpText, true);
				subtitlesHelpTextField.height = 800.0f;

				// *****************************
				// Actions to allow scripting
				// *****************************
				_setFadeColor = new JSONStorableColor("Change fade color", hsvcFa, jsc => _setFadeColor.val = jsc.val) { isStorable = false };
				_setFadeInTime = new JSONStorableFloat("Change fade in time", 5, val => _fadeInTime.val = val, 0f, 120.0f) { isStorable = false };
				_setFadeOutTime = new JSONStorableFloat("Change fade out time", 5, val => _fadeOutTime.val = val, 0f, 120.0f) { isStorable = false };
				_setSubtitlesColor = new JSONStorableColor("Change subtitles color", hsvcSt, jsc => _subtitlesColor.val = jsc.val) { isStorable = false };
				_setSubtitlesSize = new JSONStorableFloat("Change subtitles size", 18, val => _subtitlesSize.val = val, 18.0f, 100.0f) { isStorable = false };
				_setSubtitlesFont = new JSONStorableStringChooser(
					"Change subtitles font",
					_fontChoices,
					_fontChoices[0],
					"Change subtitles font",
					val => _subtitlesFontChoice.val = val
				) { isStorable = false };
				_setSubtitlesAlignment = new JSONStorableStringChooser(
					"Change subtitles alignment",
					new List<string>
					{
						"Bottom",
						"Center",
						"Top"
					},
					"Bottom",
					"Change subtitles alignment",
					val => _subtitleAlignmentChoice.val = val
				) { isStorable = false };
				_setSubtitlesFadeDuration = new JSONStorableFloat("Change subtitles cross fade duration", 1f, val => _subtitlesFadeDuration.val = val, 0f, 10f, false) { isStorable = false };
				_setSubtitlesShowDuration = new JSONStorableFloat("Change subtitles show duration", 5f, val => _subtitlesShowDuration.val = val, 0f, 20f, false) { isStorable = false };

				// Actions
				_triggerFadeIn = new JSONStorableAction("Start Fade In", FadeIn);
				_triggerFadeOut = new JSONStorableAction("Start Fade Out", FadeOut);
				_triggerFadeInInstant = new JSONStorableAction("Fade In Instant", FadeInInstant);
				_triggerFadeOutInstant = new JSONStorableAction("Fade Out Instant", FadeOutInstant);
				_showSubtitles5Secs = new JSONStorableAction("Show subtitles for 5secs", DoShowSubtitles5Secs);
				_showSubtitlesPermanent = new JSONStorableAction("Show subtitles permanently", DoShowSubtitlesPermanent);
				_setSubtitlesTextAndShowNow = new JSONStorableString("Set subtitles text and show now", "", SetSubtitlesTextAndShowNow) { isStorable = false };
				_hideSubtitles = new JSONStorableAction("Hide subtitles", DoHideSubtitles);

				// Fake actions to split things the user can use safely
				var fakeFuncUseBelow = new JSONStorableAction("- - - - Use these functions below ↓ - - - - -", () => { });
				var fakeFuncDoNotUseBelow = new JSONStorableAction("- - - - Avoid using these functions below ↓ - - - - -", () => { });

				// **********************************************************************************************************************************
				// Registering variables, a bit strange to disconnect them from the initial creation, but allows me to order them in the action list
				// **********************************************************************************************************************************
				RegisterAction(fakeFuncUseBelow);
				RegisterAction(_triggerFadeIn);
				RegisterAction(_triggerFadeOut);
				RegisterAction(_triggerFadeInInstant);
				RegisterAction(_triggerFadeOutInstant);
				RegisterString(_setSubtitlesTextAndShowNow);

				RegisterColor(_setFadeColor);
				RegisterFloat(_setFadeInTime);
				RegisterFloat(_setFadeOutTime);

				RegisterString(_subtitlesText);
				RegisterColor(_setSubtitlesColor);
				RegisterFloat(_setSubtitlesSize);
				RegisterStringChooser(_setSubtitlesFont);
				RegisterStringChooser(_setSubtitlesAlignment);
				RegisterFloat(_setSubtitlesFadeDuration);
				RegisterFloat(_setSubtitlesShowDuration);
				RegisterAction(_showSubtitles5Secs);
				RegisterAction(_showSubtitlesPermanent);
				RegisterAction(_hideSubtitles);

				// JSONStorable Variables, the user should not use these without changing the save
				RegisterAction(fakeFuncDoNotUseBelow);
				RegisterColor(_fadeColor);
				RegisterBool(_fadeAtStart);
				RegisterBool(_fadeInOnLoadComplete);
				RegisterFloat(_fadeInTime);
				RegisterFloat(_fadeOutTime);

				RegisterColor(_subtitlesColor);
				RegisterFloat(_subtitlesSize);
				RegisterStringChooser(_subtitlesFontChoice);
				RegisterStringChooser(_subtitleAlignmentChoice);
				RegisterFloat(_subtitlesFadeDuration);
				RegisterFloat(_subtitlesShowDuration);
			}
			catch (Exception e)
			{
				SuperController.LogError("VAMOverlays: " + e);
			}
		}

		private void Start()
		{
			try
			{
				InitFadeObjects();
				// If we fade after scene load
				if (_fadeAtStart.val)
				{
					// Make the fade layer opaque
					_fadeImg.canvasRenderer.SetAlpha(1.0f);

					if (_fadeInOnLoadComplete.val)
					{
						StartCoroutine(FadeInOnLoadCompleteCo());
					}
				}
			}
			catch (Exception e)
			{
				SuperController.LogError("VAMOverlays: " + e);
			}
		}

		private IEnumerator FadeInOnLoadCompleteCo()
		{
			while (SuperController.singleton.isLoading)
				yield return 0;
			FadeIn();
		}

#if(DEBUG)
		private void Update()
		{
			_debugArea.val = $"<b>DEBUG</b>\nVR enabled : {(_isPlayerInVr ? "yes" : "no")} \n";
		}
#endif

		// **************************
		// Functions
		// **************************

		private static string GetRandomQuote()
		{
			var random = new System.Random();
			var quoteIndex = random.Next(SubtitlesQuotes.Count);
			return SubtitlesQuotes[quoteIndex];
		}

		// Global FadeIn Function
		private void FadeIn()
		{
			_fadeImg.canvasRenderer.SetAlpha(1.0f);
			_fadeImg.CrossFadeAlpha(0.0f, _fadeInTime.val, false);
		}

		private void FadeInInstant()
		{
			_fadeImg.canvasRenderer.SetAlpha(0.0f);
		}

		// Triggers the FadeIn when you click on the test button
		private void TestFadeIn()
		{
			FadeIn();
		}

		// Global FadeOut Function
		private void FadeOut()
		{
			_fadeImg.canvasRenderer.SetAlpha(0.0f);
			_fadeImg.CrossFadeAlpha(1.0f, _fadeOutTime.val, false);
		}

		private void FadeOutInstant()
		{
			_fadeImg.canvasRenderer.SetAlpha(1.0f);
		}

		// Triggers the FadeOut when you click on the test button
		private void TestFadeOut()
		{
			FadeOut();
			Invoke(nameof(FadeIn), _fadeOutTime.val + 3.0f);
		}

		private void ChangeSubtitlesFont(string fontVal)
		{
			if (_subtitlesTxt == null) return;
			Font font;
			if (_fontAssets.TryGetValue(fontVal, out font))
				_subtitlesTxt.font = font;
			_setSubtitlesFont.valNoCallback = fontVal;
		}

		private void ChangeSubtitlesAlignment(string alignmentVal)
		{
			if (_subtitlesTxt == null) return;
			switch (alignmentVal)
			{
				case "Center":
					_subtitlesTxt.alignment = TextAnchor.MiddleCenter;
					break;
				case "Top":
					_subtitlesTxt.alignment = TextAnchor.UpperCenter;
					break;
				default:
					_subtitlesTxt.alignment = TextAnchor.LowerCenter;
					break;
			}
		}

		private void InitFadeObjects()
		{
			// ******************************
			// CREATION OF THE ELEMENTS
			// ******************************
			// Creation of the main Canvas
			_vamOverlaysGO = new GameObject("FadeCanvas")
			{
				transform =
				{
					localRotation = Quaternion.identity,
					localPosition = new Vector3(0, 0, 0)
				},
				layer = 5
			};
			_overlaysCanvas = _vamOverlaysGO.AddComponent<Canvas>();
			_overlaysCanvas.renderMode = RenderMode.WorldSpace;
			_overlaysCanvas.sortingOrder = 2;
			_overlaysCanvas.planeDistance = 10.0f;
			_vamOverlaysGO.AddComponent<CanvasScaler>();

			// Configuration of the RectTransform of the Canvas
			var canvasRecTr = _vamOverlaysGO.GetComponent<RectTransform>();
			canvasRecTr.sizeDelta = new Vector2(2560f, 1440f);
			canvasRecTr.localPosition = new Vector3(0, 0, 0.8f); // was 0.3f initially
			canvasRecTr.localScale = new Vector3(0.00024f, 0.00024f, 1.0f);

			// Creation of the structure for the fade
			_fadeImgGO = new GameObject("FadeImage")
			{
				layer = 5
			};
			_fadeImgGO.transform.SetParent(_vamOverlaysGO.transform, false);

			_fadeImgGO.AddComponent<CanvasRenderer>();
			_fadeImg = _fadeImgGO.AddComponent<Image>();
			_fadeImg.raycastTarget = false;
			_fadeImg.canvasRenderer.SetAlpha(0.0f);
			_fadeImg.color = _fadeColor.colorPicker.currentColor;

			// Configuration of the RectTransform of the fade
			var fadeImgRecTr = _fadeImg.GetComponent<RectTransform>();

			fadeImgRecTr.localPosition = new Vector3(0, 0, 0);
			fadeImgRecTr.localScale = new Vector3(1, 1, 1);
			fadeImgRecTr.localRotation = Quaternion.identity;
			fadeImgRecTr.anchorMin = new Vector2(0, 0);
			fadeImgRecTr.anchorMax = new Vector2(1, 1);
			fadeImgRecTr.offsetMin = new Vector2(-4000.0f, -4000.0f);
			fadeImgRecTr.offsetMax = new Vector2(4000.0f, 4000.0f);

			// Creation of the structure for subtitles
			_subtitlesTxtGO = new GameObject("SubtitlesText")
			{
				layer = 5
			};
			_subtitlesTxtGO.transform.SetParent(_vamOverlaysGO.transform, false);

			_subtitlesTxtGO.AddComponent<CanvasRenderer>();
			_subtitlesTxt = _subtitlesTxtGO.AddComponent<Text>();
			_subtitlesTxtShadow = _subtitlesTxtGO.AddComponent<Shadow>();

			// Defining text properties
			_subtitlesTxt.raycastTarget = false;
			_subtitlesTxt.canvasRenderer.SetAlpha(0.0f);
			_subtitlesTxt.color = _subtitlesColor.colorPicker.currentColor;
			_subtitlesTxt.text = "";

			_subtitlesTxt.font = _fontAssets["Arial"];

			// Selecting the default alignment
			ChangeSubtitlesAlignment(_subtitleAlignmentChoice.val);

			// Defining shadow properties
			_subtitlesTxtShadow.effectColor = Color.black;
			_subtitlesTxtShadow.effectDistance = new Vector2(2f, -0.5f);

			// And finally again, RectTransform tweaks
			_subtitlesRecTr = _subtitlesTxt.GetComponent<RectTransform>();
			_subtitlesRecTr.localPosition = new Vector3(0, 0, 0);
			_subtitlesRecTr.localScale = new Vector3(1, 1, 1);
			_subtitlesRecTr.localRotation = Quaternion.identity;
			_subtitlesRecTr.anchorMin = new Vector2(0, 0);
			_subtitlesRecTr.anchorMax = new Vector2(1, 1);

			SyncVRMode();
		}

		private void SyncFontSize()
		{
			if (_subtitlesTxt == null) return;
			var size = _subtitlesSize.val;
			var finalSize = size * 2.34f; // original value * tweak for updates (to avoid breaking old scenes)
			if (SuperController.singleton.centerCameraTarget.targetCamera != SuperController.singleton.MonitorCenterCamera)
			{
				finalSize = size * 5f; // VR multiplier - was 2.5f
			}

			_subtitlesTxt.fontSize = (int)Math.Round(finalSize);
		}

		private void SyncOverlay()
		{
			if (_subtitlesRecTr == null) return;
			var cam = SuperController.singleton.centerCameraTarget.targetCamera;
			if (cam == null) return;
			_vamOverlaysGO.transform.SetParent(cam.transform, false);
			_overlaysCanvas.worldCamera = cam;
			if (cam == SuperController.singleton.MonitorCenterCamera)
			{
				// Desktop config
				_subtitlesRecTr.offsetMin = new Vector2(300.0f, -200.0f);
				_subtitlesRecTr.offsetMax = new Vector2(-300.0f, 200.0f);
			}
			else
			{
				// VR Configs
				_subtitlesRecTr.offsetMin = new Vector2(370.0f, 0.0f); // Was 280f initially
				_subtitlesRecTr.offsetMax = new Vector2(-370.0f, 0.0f);
			}
		}

		private void SyncVRMode()
		{
			SyncOverlay();
			SyncFontSize();
		}

		private void DoShowSubtitles5Secs()
		{
			CancelInvoke(nameof(DoHideSubtitles));
			if (_subtitlesTxt == null) return;
			_subtitlesTxt.canvasRenderer.SetAlpha(0.0f);
			_subtitlesTxt.CrossFadeAlpha(1.0f, _subtitlesFadeDuration.val, false);
			Invoke(nameof(DoHideSubtitles), 5 + _subtitlesFadeDuration.val);
		}

		private void DoShowSubtitlesPermanent()
		{
			CancelInvoke(nameof(DoHideSubtitles));
			SyncVRMode();
			if (_subtitlesTxt == null) return;
			_subtitlesTxt.canvasRenderer.SetAlpha(0.0f);
			_subtitlesTxt.CrossFadeAlpha(1.0f, _subtitlesFadeDuration.val, false);
		}

		private void SetSubtitlesTextAndShowNow(string text)
		{
			CancelInvoke(nameof(DoHideSubtitles));
			_subtitlesText.valNoCallback = text;
			_setSubtitlesTextAndShowNow.valNoCallback = "";
			SyncVRMode();
			if (_subtitlesTxt == null) return;
			_subtitlesTxt.text = _subtitlesText.val;
			_subtitlesTxt.canvasRenderer.SetAlpha(0.0f);
			_subtitlesTxt.CrossFadeAlpha(1.0f, _subtitlesFadeDuration.val, false);
			Invoke(nameof(DoHideSubtitles), _subtitlesShowDuration.val + _subtitlesFadeDuration.val);
		}

		private void DoHideSubtitles()
		{
			if (_subtitlesTxt == null) return;
			_subtitlesTxt.CrossFadeAlpha(0.0f, _subtitlesFadeDuration.val, false);
		}

		// **************************
		// Callbacks
		// **************************
		private void OnFontsBundleLoaded(Request aRequest)
		{
			// Loading font assets
			foreach (var fontKvp in FontList)
			{
				if (fontKvp.Value == null) continue;
				var fnt = aRequest.assetBundle.LoadAsset<Font>(fontKvp.Value);
				if (fnt == null) continue;
				// Adding font asset if successfully loaded in the assets
				_fontAssets.Add(fontKvp.Key, fnt);
				// If we have a value set in the custom UI that is the same as our font, let's update the Canvas
				if (_subtitleAlignmentChoice.val == fontKvp.Key)
				{
					ChangeSubtitlesFont(fontKvp.Key);
				}
			}

		}

		private void FadeColorCallback(JSONStorableColor selectedColor)
		{
			if (_fadeImg == null) return;
			_fadeImg.color = HSVColorPicker.HSVToRGB(selectedColor.val);
			_setFadeColor.valNoCallback = selectedColor.val;
		}

		private void FadeAtStartCallback(bool val)
		{
			if (!val)
				_fadeInOnLoadComplete.valNoCallback = false;
		}

		private void FadeInOnLoadCompleteCallback(bool val)
		{
			if (val)
				_fadeAtStart.valNoCallback = true;
		}

		private void SubtitlesColorCallback(JSONStorableColor selectedColor)
		{
			if (_subtitlesTxt == null) return;
			_subtitlesTxt.color = HSVColorPicker.HSVToRGB(selectedColor.val);
			_setSubtitlesColor.valNoCallback = selectedColor.val;
		}

		private void PreviewSubtitlesCallback(JSONStorableBool state)
		{
			if (_subtitlesTxt == null) return;
			if (state.val)
			{
				CancelInvoke(nameof(DoHideSubtitles));
				SyncVRMode();
				_subtitlesTxt.text = GetRandomQuote();
				_subtitlesTxt.canvasRenderer.SetAlpha(1.0f);
			}
			else
			{
				_subtitlesTxt.canvasRenderer.SetAlpha(0.0f);
			}
		}

		private void SubtitlesSizeCallback(float val)
		{
			_subtitlesSize.valNoCallback = Mathf.Round(val);
			_setSubtitlesSize.valNoCallback = _subtitlesSize.val;
			SyncFontSize();
		}

		private void SubtitlesValueCallback(JSONStorableString text)
		{
			if (_subtitlesTxt == null) return;
			_subtitlesTxt.text = _subtitlesText.val;
		}

		private void SubtitlesAlignmentCallback(string alignmentChoice)
		{
			ChangeSubtitlesAlignment(alignmentChoice);
		}

		// **************************
		// Time to cleanup !
		// **************************
		private void OnDestroy()
		{
			// Removing the main object, every children will be destroyed too
			Destroy(_vamOverlaysGO);

			// Font bundle unload
			AssetLoader.DoneWithAssetBundleFromFile(_fontsBundlePath);
		}

		// ***********************************************************
		// EXTERNAL TOOL - Thank you great coders for your content!
		// ***********************************************************

		// *********** MacGruber_Utils.cs START *********************
		// Get directory path where the plugin is located. Based on Alazi's & VAMDeluxe method.
		private static string GetPluginPath(MVRScript self)
		{
			var id = self.name.Substring(0, self.name.IndexOf('_'));
			var filename = self.manager.GetJSON()["plugins"][id].Value;
			return filename.Substring(0, filename.LastIndexOfAny(new[] { '/', '\\' }));
		}
		// *********** MacGruber_Utils.cs END *********************
	}
}