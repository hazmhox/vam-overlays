// #define DEBUG
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using MeshVR;
using Request = MeshVR.AssetLoader.AssetBundleFromFileRequest;

// VAMOverlays
// Overlays system to display fades, titles and subtitles

namespace VAMOverlaysPlugin
{
	public class VAMOverlays : MVRScript
	{
		private string _pluginPath;
		private string _assetsPath;
		private string _fontsBundlePath;

		// Game Objects and components
		private GameObject _vamOverlaysGO;
		private Canvas _overlaysCanvas;

		private GameObject _fadeImgGO;
		private Image _fadeImg;

		private GameObject _subtitlesTxtGO;
		private Text _subtitlesTxt;
		private Shadow _subtitlesTxtShadow;

		private JSONStorableBool _fadeAtStart;
		private JSONStorableColor _fadeColor;
		private JSONStorableFloat _fadeInTime;
		private JSONStorableFloat _fadeOutTime;

		private JSONStorableColor _subtitlesColor;
		private JSONStorableString _subtitlesText;
		private JSONStorableFloat _subtitlesSize;
		private JSONStorableStringChooser _subtitlesFontChoice;
		private JSONStorableStringChooser _subtitleAlignmentChoice;

		private JSONStorableAction _triggerFadeIn;
		private JSONStorableAction _triggerFadeOut;
		private JSONStorableColor _setFadeColor;
		private JSONStorableFloat _setFadeInTime;
		private JSONStorableFloat _setFadeOutTime;

		private JSONStorableColor _setSubtitlesColor;
		private JSONStorableFloat _setSubtitlesSize;
		private JSONStorableStringChooser _setSubtitlesFont;
		private JSONStorableStringChooser _setSubtitlesAlignment;
		private JSONStorableAction _showSubtitles5Secs;
		private JSONStorableAction _showSubtitlesPermanent;
		private JSONStorableAction _hideSubtitles;

		private Color _currentFadeColor;
		private Color _currentSubtitlesColor;
		private float _currentFadeInTime;
		private float _currentFadeOutTime;

		private JSONStorableString _debugArea;

		private bool _isPlayerInVr;

		private List<string> _subtitlesQuotes;
		private List<string> _alignmentChoices;
		private List<string> _fontChoices;

		private Dictionary<string, string> _fontList;
		private Dictionary<string, Font> _fontAssets;

		public override void Init()
		{
			try
			{
				if (containingAtom.type != "Empty")
				{
					SuperController.LogError("Please add VAMOverlays on an Empty Atom");
					return;
				}

				_pluginPath = GetPluginPath(this);
				_assetsPath = _pluginPath + "/assets";
				_fontsBundlePath = _assetsPath + "/fonts.fontbundle";

				// LISTS AND DICT
				// Random quotes
				_subtitlesQuotes = new List<string>
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

				// Alignments
				_alignmentChoices = new List<string>
				{
					"Bottom",
					"Center",
					"Top"
				};

				// Fonts paths in the bundle. I'm lazy, and I don't think I do have benefits from loading a json file just for simple paths
				_fontList = new Dictionary<string, string>
				{
					// ReSharper disable StringLiteralTypo
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

				// Fonts Assets, i'm creating Arial by default because this font rocks for subtitles, and if the bundle fails, we have at least one font
				_fontAssets = new Dictionary<string, Font>
				{
					["Arial"] = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf")
				};

				// Creating font choices based on the dict
				_fontChoices = new List<string>
				{
					"Arial" // Adding Arial by default
				};
				foreach (var fontKvp in _fontList)
				{
					// Kinda hacky, because I cannot find a way to update a popup after creating it
					// I'm gonna assume that there is never gonna be any problem to load the bundle :x
					_fontChoices.Add(fontKvp.Key);
				}

				// Loading the bundle for the current selected voice
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
				var fadeTitle = new JSONStorableString("Help",
					"<color=#000><size=30><b>Fading settings</b></size></color>\nChange the appearance and behavior of the default fade");
				var fadeTitleField = CreateTextField(fadeTitle, false);
				fadeTitleField.height = 50.0f;

				// Color picker
				var hsvcFa = HSVColorPicker.RGBToHSV(0f, 0f, 0f);
				_fadeColor = new JSONStorableColor("Fade Color", hsvcFa, FadeColorCallback);
				CreateColorPicker(_fadeColor);

				// Fade at start enabled ?
				_fadeAtStart = new JSONStorableBool("Fade at start", false)
				{
					storeType = JSONStorableParam.StoreType.Full
				};
				CreateToggle(_fadeAtStart, false);

				// Fade in time
				_fadeInTime = new JSONStorableFloat("Fade in time", 5.0f, 0f, 120.0f, true, true)
				{
					storeType = JSONStorableParam.StoreType.Full
				};
				CreateSlider(_fadeInTime, false);

				// Fade out time
				_fadeOutTime = new JSONStorableFloat("Fade out time", 5.0f, 0f, 120.0f, true, true)
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
				var helpText = new JSONStorableString("Help",
					"<color=#000><size=35><b>Fading help</b></size></color>\n\n" +
					"<color=#333>" +
					"<b>Fade color :</b> The color of the fade effect\n\n" +
					"<b>Fade at start :</b> If the fade should cover the screen when the scene has finished loading\nIf you don't control the fade in afterwards, the scene will stay black (or the color you have selected) and the player will only be able to see the menus.\n\n" +
					"<b>Fade in time :</b> How much time it takes to fade from the color being opaque to completely transparent.\n\n" +
					"<b>Fade out time :</b> How much time it takes to fade from the color being completely transparent to opaque.\n\n" +
					"</color>"
				);
				var helpTextfield = CreateTextField(helpText, false);
				helpTextfield.height = 800.0f;

				// **************************************************
				// ** RIGHT : Subtitles properties
				// **************************************************
				_subtitlesText = new JSONStorableString("Set subtitles text", "", SubtitlesValueCallback) { isStorable = false };

				// Title subtitles
				var subtitlesTitle = new JSONStorableString("Help", "<color=#000><size=30><b>Subtitles settings</b></size></color>\nChange the appearance and behavior of the default subtitles");
				var subtitlesTitleField = CreateTextField(subtitlesTitle, true);
				subtitlesTitleField.height = 20.0f;

				// Color picker
				var hsvcSt = HSVColorPicker.RGBToHSV(1f, 1f, 1f);
				_subtitlesColor = new JSONStorableColor("Subtitles Color", hsvcSt, SubtitlesColorCallback);
				CreateColorPicker(_subtitlesColor, true);

				// Preview
				var previewSubtitles = new JSONStorableBool("Preview subtitles", false, PreviewSubtitlesCallback);
				CreateToggle(previewSubtitles, true);

				_subtitlesSize = new JSONStorableFloat("Subtitles size", 0, (val) =>
				{
					_subtitlesSize.valNoCallback = Mathf.Round(val);
					SubtitlesSizeCallback(_subtitlesSize.val);
				}, 18.0f, 100.0f);
				CreateSlider(_subtitlesSize, true);

				_subtitlesFontChoice = new JSONStorableStringChooser("Font", _fontChoices, "Arial", "Font", SubtitlesFontCallback);
				var sfcUdp = CreateScrollablePopup(_subtitlesFontChoice, true);
				sfcUdp.labelWidth = 150f;

				_subtitleAlignmentChoice = new JSONStorableStringChooser("Text alignment", _alignmentChoices, "Bottom", "Text alignment", SubtitlesAlignmentCallback);
				var sacUdp = CreateScrollablePopup(_subtitleAlignmentChoice, true);
				sacUdp.labelWidth = 300f;

				// *****************************
				// Actions to allow scripting
				// *****************************
				_triggerFadeIn = new JSONStorableAction("Start Fade In", FadeIn);
				_triggerFadeOut = new JSONStorableAction("Start Fade Out", FadeOut);
				_setFadeColor = new JSONStorableColor("Change fade color", hsvcFa, FadeColorCallback) { isStorable = false };
				_setFadeInTime = new JSONStorableFloat("Change fade in time", 5, SetFadeInTimeCallback, 0f, 120.0f) { isStorable = false };
				_setFadeOutTime = new JSONStorableFloat("Change fade out time", 5, SetFadeOutTimeCallback, 0f, 120.0f) { isStorable = false };
				_setSubtitlesColor = new JSONStorableColor("Change subtitles color", hsvcSt, SubtitlesColorCallback) { isStorable = false };
				_setSubtitlesSize = new JSONStorableFloat("Change subtitles size", 18, (val) =>
				{
					_setSubtitlesSize.valNoCallback = Mathf.Round(val);
					SubtitlesSizeCallback(_setSubtitlesSize.val);
				}, 18.0f, 100.0f) { isStorable = false };
				_setSubtitlesFont = new JSONStorableStringChooser("Change subtitles font", _fontChoices, "Arial", "Change subtitles font",
						choice => SubtitlesFontCallback(choice.val))
					{ isStorable = false };
				_setSubtitlesAlignment = new JSONStorableStringChooser("Change subtitles alignment", _alignmentChoices, "Bottom", "Change subtitles alignment",
					choice => SubtitlesAlignmentCallback(choice.val)) { isStorable = false };
				_showSubtitles5Secs = new JSONStorableAction("Show subtitles for 5secs", DoShowSubtitles5Secs);
				_showSubtitlesPermanent = new JSONStorableAction("Show subtitles permanently", DoShowSubtitlesPermanent);
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
				RegisterColor(_setFadeColor);
				RegisterFloat(_setFadeInTime);
				RegisterFloat(_setFadeOutTime);

				RegisterString(_subtitlesText);
				RegisterColor(_setSubtitlesColor);
				RegisterFloat(_setSubtitlesSize);
				RegisterStringChooser(_setSubtitlesFont);
				RegisterStringChooser(_setSubtitlesAlignment);
				RegisterAction(_showSubtitles5Secs);
				RegisterAction(_showSubtitlesPermanent);
				RegisterAction(_hideSubtitles);

				// JSONStorable Variables, the user should not use these without changing the save
				RegisterAction(fakeFuncDoNotUseBelow);
				RegisterColor(_fadeColor);
				RegisterBool(_fadeAtStart);
				RegisterFloat(_fadeInTime);
				RegisterFloat(_fadeOutTime);

				RegisterColor(_subtitlesColor);
				RegisterFloat(_subtitlesSize);
				RegisterStringChooser(_subtitlesFontChoice);
				RegisterStringChooser(_subtitleAlignmentChoice);

				// Settings "current" variables
				_currentFadeColor = _fadeColor.colorPicker.currentColor;
				_currentSubtitlesColor = _subtitlesColor.colorPicker.currentColor;
				_currentFadeInTime = _fadeInTime.val;
				_currentFadeOutTime = _fadeOutTime.val;

				// Initializing my VR flag (maybe at some point I'll need to make some different checks, so I'm making that in advance)
				_isPlayerInVr = XRDevice.isPresent;

			}
			catch (Exception e)
			{
				SuperController.LogError("VAMOverlays - Exception caught: " + e);
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
				}
			}
			catch (Exception e)
			{
				SuperController.LogError("VAMOverlays - Exception caught: " + e);
			}
		}

		private void Update()
		{
			UpdateDebugArea();
		}

		// **************************
		// Functions
		// **************************

		[Conditional("DEBUG")]
		private void UpdateDebugArea()
		{
#if(DEBUG)
			_debugArea.val = $"<b>DEBUG</b>\nVR enabled : {(_isPlayerInVr ? "yes" : "no")} \n";
#endif
		}

		private string GETRandomQuote()
		{
			var random = new System.Random();
			var quoteIndex = random.Next(_subtitlesQuotes.Count);
			return _subtitlesQuotes[quoteIndex];
		}

		// Global FadeIn Function
		private void FadeIn()
		{
			_fadeImg.canvasRenderer.SetAlpha(1.0f);
			_fadeImg.CrossFadeAlpha(0.0f, _currentFadeInTime, false);
		}

		// Triggers the FadeIn when you click on the test button
		private void TestFadeIn()
		{
			// Make the fade layer opaque
			_fadeImg.canvasRenderer.SetAlpha(1.0f);
			_fadeImg.CrossFadeAlpha(0.0f, _fadeInTime.val, false);
		}

		// Global FadeOut Function
		private void FadeOut()
		{
			_fadeImg.canvasRenderer.SetAlpha(0.0f);
			_fadeImg.CrossFadeAlpha(1.0f, _currentFadeOutTime, false);
		}

		// Triggers the FadeOut when you click on the test button
		private void TestFadeOut()
		{
			_fadeImg.canvasRenderer.SetAlpha(0.0f);
			_fadeImg.CrossFadeAlpha(1.0f, _fadeOutTime.val, false);
			Invoke(nameof(FadeIn), _fadeOutTime.val + 5.0f);
		}

		private void ChangeSubtitlesFont(string fontVal)
		{
			if (_subtitlesTxt != null)
			{
				_subtitlesTxt.font = _fontAssets[fontVal];
			}
		}

		private void ChangeSubtitlesAlignment(string alignmentVal)
		{
			if (_subtitlesTxt != null)
			{
				if (alignmentVal == "Center")
				{
					_subtitlesTxt.alignment = TextAnchor.MiddleCenter;
				}
				else if (alignmentVal == "Top")
				{
					_subtitlesTxt.alignment = TextAnchor.UpperCenter;
				}
				else
				{
					_subtitlesTxt.alignment = TextAnchor.LowerCenter;
				}
			}
		}

		private int GETFontSize(float size)
		{
			var finalSize = size * 2.34f; // original value * tweak for updates (to avoid breaking old scenes)
			if (_isPlayerInVr)
			{
				finalSize = size * 5f; // VR multiplier - was 2.5f
			}

			return (int)Math.Round(finalSize);
		}

		private void InitFadeObjects()
		{
			try
			{
				// ******************************
				// CREATION OF THE ELEMENTS
				// ******************************
				// Creation of the main Canvas
				_vamOverlaysGO = new GameObject
				{
					name = "FadeCanvas"
				};
				_vamOverlaysGO.transform.SetParent(Camera.main.transform);
				_vamOverlaysGO.transform.localRotation = Quaternion.identity;
				_vamOverlaysGO.transform.localPosition = new Vector3(0, 0, 0);
				_vamOverlaysGO.layer = 5;
				_overlaysCanvas = _vamOverlaysGO.AddComponent<Canvas>();
				_overlaysCanvas.renderMode = RenderMode.WorldSpace;
				_overlaysCanvas.sortingOrder = 2;
				_overlaysCanvas.worldCamera = Camera.main;
				_overlaysCanvas.planeDistance = 10.0f;
				_vamOverlaysGO.AddComponent<CanvasScaler>();

				// Configuration of the RectTransform of the Canvas
				var canvasRecTr = _vamOverlaysGO.GetComponent<RectTransform>();
				canvasRecTr.sizeDelta = new Vector2(2560f, 1440f);
				canvasRecTr.localPosition = new Vector3(0, 0, 0.8f); // wav 0.3f initially
				canvasRecTr.localScale = new Vector3(0.00024f, 0.00024f, 1.0f);

				// Creation of the structure for the fade
				_fadeImgGO = new GameObject("FadeImage")
				{
					layer = 5
				};
				_fadeImgGO.transform.SetParent(_vamOverlaysGO.transform);

				_fadeImgGO.AddComponent<CanvasRenderer>();
				_fadeImg = _fadeImgGO.AddComponent<Image>();
				_fadeImg.raycastTarget = false;
				_fadeImg.canvasRenderer.SetAlpha(0.0f);
				_fadeImg.color = _currentFadeColor;

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
				_subtitlesTxtGO.transform.SetParent(_vamOverlaysGO.transform);

				_subtitlesTxtGO.AddComponent<CanvasRenderer>();
				_subtitlesTxt = _subtitlesTxtGO.AddComponent<Text>();
				_subtitlesTxtShadow = _subtitlesTxtGO.AddComponent<Shadow>();

				// Defining text properties
				_subtitlesTxt.raycastTarget = false;
				_subtitlesTxt.canvasRenderer.SetAlpha(0.0f);
				_subtitlesTxt.color = _currentSubtitlesColor;
				_subtitlesTxt.text = GETRandomQuote();

				_subtitlesTxt.font = _fontAssets["Arial"];

				_subtitlesTxt.fontSize = GETFontSize(18); // Will deal automatically with the ratio depending on the VR or desktop state - Was 18 initially

				// Selecting the default alignment
				ChangeSubtitlesAlignment(_subtitleAlignmentChoice.val);

				// Defining shadow properties
				_subtitlesTxtShadow.effectColor = Color.black;
				_subtitlesTxtShadow.effectDistance = new Vector2(2f, -0.5f);

				// And finally again, RectTransform tweaks
				var subtitlesRecTr = _subtitlesTxt.GetComponent<RectTransform>();
				subtitlesRecTr.localPosition = new Vector3(0, 0, 0);
				subtitlesRecTr.localScale = new Vector3(1, 1, 1);
				subtitlesRecTr.localRotation = Quaternion.identity;
				subtitlesRecTr.anchorMin = new Vector2(0, 0);
				subtitlesRecTr.anchorMax = new Vector2(1, 1);

				// **********************************
				// SETUP BASED ON VR OR DESKTOP MODE
				// **********************************

				// VR Configs
				if (_isPlayerInVr)
				{
					subtitlesRecTr.offsetMin = new Vector2(370.0f, 0.0f); // Was 280f initially
					subtitlesRecTr.offsetMax = new Vector2(-370.0f, 0.0f);
					// Desktop configs
				}
				else
				{
					subtitlesRecTr.offsetMin = new Vector2(300.0f, -200.0f);
					subtitlesRecTr.offsetMax = new Vector2(-300.0f, 200.0f);
				}
			}
			catch (Exception e)
			{
				SuperController.LogError("VAMOverlays - Exception caught will initializing: " + e);
			}
		}

		private void DoShowSubtitles5Secs()
		{
			if (_subtitlesTxt != null)
			{
				_subtitlesTxt.canvasRenderer.SetAlpha(0.0f);
				_subtitlesTxt.CrossFadeAlpha(1.0f, 1.0f, false);
				Invoke(nameof(DoHideSubtitles), 6.0f);
			}
		}

		private void DoShowSubtitlesPermanent()
		{
			if (_subtitlesTxt != null)
			{
				_subtitlesTxt.canvasRenderer.SetAlpha(0.0f);
				_subtitlesTxt.CrossFadeAlpha(1.0f, 1.0f, false);
			}
		}

		private void DoHideSubtitles()
		{
			if (_subtitlesTxt != null)
			{
				_subtitlesTxt.CrossFadeAlpha(0.0f, 1.0f, false);
			}
		}

		// **************************
		// Callbacks
		// **************************
		private void OnFontsBundleLoaded(Request aRequest)
		{
			// Loading font assets
			foreach (var fontKvp in _fontList)
			{
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
			if (_fadeImg != null)
			{
				_fadeImg.color = HSVColorPicker.HSVToRGB(selectedColor.val);
				_currentFadeColor = HSVColorPicker.HSVToRGB(selectedColor.val);
			}
		}

		private void SetFadeInTimeCallback(float fadeInTime)
		{
			_currentFadeInTime = fadeInTime;
		}

		private void SetFadeOutTimeCallback(float fadeOutTime)
		{
			_currentFadeOutTime = fadeOutTime;
		}

		private void SubtitlesColorCallback(JSONStorableColor selectedColor)
		{
			if (_subtitlesTxt != null)
			{
				_subtitlesTxt.color = HSVColorPicker.HSVToRGB(selectedColor.val);
				_currentSubtitlesColor = HSVColorPicker.HSVToRGB(selectedColor.val);
			}
		}

		private void PreviewSubtitlesCallback(JSONStorableBool state)
		{
			if (_subtitlesTxt != null)
			{
				if (state.val)
				{
					_subtitlesTxt.text = GETRandomQuote();
					_subtitlesTxt.canvasRenderer.SetAlpha(1.0f);
				}
				else
				{
					_subtitlesTxt.canvasRenderer.SetAlpha(0.0f);
				}
			}
		}

		private void SubtitlesValueCallback(JSONStorableString text)
		{
			if (_subtitlesTxt != null)
			{
				_subtitlesTxt.text = _subtitlesText.val;
			}
		}

		private void SubtitlesSizeCallback(float subtitlesSize)
		{
			if (_subtitlesTxt != null)
			{
				_subtitlesTxt.fontSize = GETFontSize(subtitlesSize);
			}
		}

		private void SubtitlesFontCallback(string fontChoice)
		{
			ChangeSubtitlesFont(fontChoice);
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