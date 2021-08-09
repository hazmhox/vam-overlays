using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;
using System.IO;
using MeshVR;
using Request = MeshVR.AssetLoader.AssetBundleFromFileRequest;

// VAMOVERLAYS v1.0
//
// Overlays system to display fades, titles and subtitles

// TESTS Spacedog
//CanvasRecTr.localPosition = new Vector3(0,0,0.8f);
//Text size : 38.0f
//SubtitlesRecTr.offsetMin = new Vector2(600.0f, 100.0f);
//SubtitlesRecTr.offsetMax = new Vector2(-600.0f, 100.0f);

namespace VAMOverlaysPlugin
{
    public class VAMOverlays : MVRScript
    {
        public static string PLUGIN_PATH;
        public static string ASSETS_PATH;
        public static string FONTS_BUNDLE_PATH;
		
		// Game Objects and components
		GameObject VAMOverlaysGO;
		Canvas OverlaysCanvas;
		CanvasScaler OverlaysCanvasScaler;
		
		GameObject FadeImgGO;
		Image FadeImg;
		
		GameObject SubtitlesTxtGO;
		Text SubtitlesTxt;
		Shadow SubtitlesTxtShadow;

		public JSONStorableBool FadeAtStart;
		public JSONStorableColor FadeColor;
		public JSONStorableFloat FadeInTime;
		public JSONStorableFloat FadeOutTime;
		
		public JSONStorableColor SubtitlesColor;
		public JSONStorableString SubtitlesText;
		public JSONStorableFloat SubtitlesSize;
		public JSONStorableStringChooser SubtitlesFontChoice;
		public JSONStorableStringChooser SubtitleAlignmentChoice;
		
		public JSONStorableAction triggerFadeIn;
		public JSONStorableAction triggerFadeOut;
		public JSONStorableColor setFadeColor;
		public JSONStorableFloat setFadeInTime;
		public JSONStorableFloat setFadeOutTime;
		
		public JSONStorableColor setSubtitlesColor;
		public JSONStorableFloat setSubtitlesSize;
		public JSONStorableStringChooser setSubtitlesFont;
		public JSONStorableStringChooser setSubtitlesAlignment;
		public JSONStorableAction showSubtitles5secs;
		public JSONStorableAction showSubtitlesPermanent;
		public JSONStorableAction hideSubtitles;
		
		protected Color CurrentFadeColor;
		protected Color CurrentSubtitlesColor;
		protected float CurrentFadeInTime;
		protected float CurrentFadeOutTime;
		
		protected bool showDebug = false;
		protected JSONStorableString debugArea;
		
		protected bool isPlayerInVr = false;
		
		protected List<string> SubtitlesQuotes;
		protected List<string> alignmentChoices;
		protected List<string> fontChoices;
		
		protected Dictionary<string, string> FontList;
		protected Dictionary<string, Font> FontAssets;
		
		public override void Init()
        {			 
            try
            {
                if (containingAtom.type != "Empty")
                {
                    SuperController.LogError("Please add VAMOverlays on an Empty Atom");
                    return;
                }
				
				PLUGIN_PATH = GetPluginPath(this);
				ASSETS_PATH = PLUGIN_PATH + "/assets";
				FONTS_BUNDLE_PATH = ASSETS_PATH + "/fonts.fontbundle";
				
				// LISTS AND DICT
				// Random quotes
				SubtitlesQuotes = new List<string>();
				SubtitlesQuotes.Add("<b>Ripley:</b> You better just start dealing with it, Hudson! Listen to me!\nHudson, just deal with it, because we need you and I'm sick of your bullshit.");
				SubtitlesQuotes.Add("<b>Apone:</b> All right, sweethearts, what are you waiting for? Breakfast in bed?\nIt's another glorious day in the Corps.");
				SubtitlesQuotes.Add("<b>Hudson:</b> We're on the express elevator to hell, going down!");
				SubtitlesQuotes.Add("<b>Vasquez:</b> You always were an asshole, Gorman.");
				SubtitlesQuotes.Add("<b>Drake:</b> They ain't pay us enough for this, man.\n<b>Dietrich:</b> Not enough to wake up every day to your face, Drake.");
				SubtitlesQuotes.Add("<b>Vasquez:</b> How many combat drops?\n<b>Gorman:</b> Uh, two. Including this one.");
				SubtitlesQuotes.Add("<b>Hicks:</b> <i>**pulls out a shotgun**</i> I like to keep this handy. For close encounters.\n<b>Frost:</b> Yeah, I heard that.");
				
				// Alignments
				alignmentChoices = new List<string>();
				alignmentChoices.Add("Bottom");
				alignmentChoices.Add("Center");
				alignmentChoices.Add("Top");
				
				// Fonts paths in the bundle. I'm lazy, and I don't think I do have benefits from loading a json file just for simple paths
				FontList = new Dictionary<string, string>();
				FontList.Add("Poppins","Assets/VAMFonts/poppins.ttf");
				FontList.Add("Oswald","Assets/VAMFonts/oswald.ttf");
				FontList.Add("GlitchInside","Assets/VAMFonts/glitch_inside.otf");
				FontList.Add("SpaceAge","Assets/VAMFonts/space_age.ttf");
				FontList.Add("PlanetKosmos","Assets/VAMFonts/planet_kosmos.ttf");
				FontList.Add("Amalia","Assets/VAMFonts/amalia.ttf");
				FontList.Add("BirdsOfParadise","Assets/VAMFonts/birds_of_paradise.ttf");
				
				// Fonts Assets, i'm creating Arial by default because this font rocks for subtitles, and if the bundle fails, we have at least one font
				FontAssets = new Dictionary<string, Font>();
				FontAssets.Add("Arial", Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font);
				
				// Creating font choices based on the dict
				fontChoices = new List<string>();
				fontChoices.Add("Arial"); // Adding Arial by default
				foreach( KeyValuePair<string, string> fontKvp in FontList )
				{
					// Kinda hacky, because I cannot find a way to update a popup after creating it
					// I'm gonna assume that there is never gonna be any problem to load the bundle :x
					fontChoices.Add(fontKvp.Key);
				}
				
				// Loading the bundle for the current selected voice
				Request request = new AssetLoader.AssetBundleFromFileRequest {path = FONTS_BUNDLE_PATH, callback = OnFontsBundleLoaded};
				AssetLoader.QueueLoadAssetBundleFromFile(request);
				
				// **************************************************
				// ** LEFT : Fade properties
				// **************************************************
				
				// Debugging
				if( showDebug == true ) {
					debugArea = new JSONStorableString("Debug","");
					UIDynamic debugAreaField = CreateTextField(debugArea, false);
					debugAreaField.height = 150.0f;
				}
				
				// Title fading
				JSONStorableString fadeTitle = new JSONStorableString("Help","<color=#000><size=30><b>Fading settings</b></size></color>\nChange the appearance and behavior of the default fade");
				UIDynamic fadeTitlefield = CreateTextField(fadeTitle, false);
				fadeTitlefield.height = 50.0f;
				
				// Color picker
				HSVColor hsvcFA = HSVColorPicker.RGBToHSV(0f, 0f, 0f);
				FadeColor = new JSONStorableColor("Fade Color", hsvcFA, FadeColorCallback);
				CreateColorPicker(FadeColor);
				
				// Fade at start enabled ?
				FadeAtStart = new JSONStorableBool("Fade at start", false);
				FadeAtStart.storeType = JSONStorableParam.StoreType.Full;
				CreateToggle(FadeAtStart, false);
							
				// Fade in time
				FadeInTime = new JSONStorableFloat("Fade in time", 5.0f, 0f, 120.0f, true, true);
				FadeInTime.storeType = JSONStorableParam.StoreType.Full;
				CreateSlider(FadeInTime, false);			
						
				// Fade out time
				FadeOutTime = new JSONStorableFloat("Fade out time", 5.0f, 0f, 120.0f, true, true);
				FadeOutTime.storeType = JSONStorableParam.StoreType.Full;
				CreateSlider(FadeOutTime, false);

				// Test Fade in
				UIDynamicButton FadeInTestButton = CreateButton("Test Fade In");
				if (FadeInTestButton != null) {
					FadeInTestButton.button.onClick.AddListener(TestFadeIn);
				}
				
				// Test fade out
				UIDynamicButton FadeOutTestButton = CreateButton("Test Fade Out");
				if (FadeOutTestButton != null) {
					FadeOutTestButton.button.onClick.AddListener(TestFadeOut);
				}
				
				// *****************************
				// HELP
				// *****************************
				JSONStorableString helpText = new JSONStorableString("Help",
					"<color=#000><size=35><b>Fading help</b></size></color>\n\n" + 
					"<color=#333>" +
					"<b>Fade color :</b> The color of the fade effect\n\n" +
					"<b>Fade at start :</b> If the fade should cover the screen when the scene has finished loading\nIf you don't control the fade in afterwards, the scene will stay black (or the color you have selected) and the player will only be able to see the menus.\n\n" +
					"<b>Fade in time :</b> How much time it takes to fade from the color being opaque to completely transparent.\n\n" +
					"<b>Fade out time :</b> How much time it takes to fade from the color being completely transparent to opaque.\n\n" +
					"</color>"
				);
				UIDynamic helpTextfield = CreateTextField(helpText, false);
				helpTextfield.height = 800.0f;
				
				// **************************************************
				// ** RIGHT : Subtitles properties
				// **************************************************
				SubtitlesText = new JSONStorableString("Set subtitles text", "", SubtitlesValueCallback){ isStorable = false };
							
				// Title subtitles
				JSONStorableString subtitlesTitle = new JSONStorableString("Help","<color=#000><size=30><b>Subtitles settings</b></size></color>\nChange the appearance and behavior of the default subtitles");
				UIDynamic subtitlesTitlefield = CreateTextField(subtitlesTitle, true);
				subtitlesTitlefield.height = 20.0f;
							
				// Color picker
				HSVColor hsvcST = HSVColorPicker.RGBToHSV(1f, 1f, 1f);
				SubtitlesColor = new JSONStorableColor("Subtitles Color", hsvcST, SubtitlesColorCallback);
				CreateColorPicker(SubtitlesColor, true);
				
				// Preview
				JSONStorableBool previewSubtitles = new JSONStorableBool("Preview subtitles", false, PreviewSubtitlesCallback);
				CreateToggle(previewSubtitles, true);
				
				SubtitlesSize = new JSONStorableFloat("Subtitles size", 0, (val) => { SubtitlesSize.valNoCallback = Mathf.Round(val); SubtitlesSizeCallback( SubtitlesSize.val ); }, 18.0f, 100.0f);
				CreateSlider(SubtitlesSize, true);
				
				SubtitlesFontChoice = new JSONStorableStringChooser("Font", fontChoices, "Arial", "Font", SubtitlesFontCallback);
				UIDynamicPopup SFCUdp = CreatePopup(SubtitlesFontChoice, true);
				SFCUdp.labelWidth = 150f;
				
				SubtitleAlignmentChoice = new JSONStorableStringChooser("Text alignment", alignmentChoices, "Bottom", "Text alignment", SubtitlesAlignementCallback);
				UIDynamicPopup SACUdp = CreatePopup(SubtitleAlignmentChoice, true);
				SACUdp.labelWidth = 300f;
				
				// *****************************
				// Actions to allow scripting
				// *****************************				
				triggerFadeIn = new JSONStorableAction("Start Fade In", () => { FadeIn(); });
				triggerFadeOut = new JSONStorableAction("Start Fade Out", () =>	{ FadeOut(); });
				setFadeColor = new JSONStorableColor("Change fade color", hsvcFA, FadeColorCallback){ isStorable = false };
				setFadeInTime = new JSONStorableFloat("Change fade in time", 5, setFadeInTimeCallback, 0f, 120.0f ){ isStorable = false };
				setFadeOutTime = new JSONStorableFloat("Change fade out time", 5, setFadeOutTimeCallback, 0f, 120.0f ){ isStorable = false };
				setSubtitlesColor = new JSONStorableColor("Change subtitles color", hsvcST, SubtitlesColorCallback ){ isStorable = false };
				setSubtitlesSize = new JSONStorableFloat("Change subtitles size", 18, (val) => { setSubtitlesSize.valNoCallback = Mathf.Round(val); SubtitlesSizeCallback( setSubtitlesSize.val ); }, 18.0f, 100.0f ){ isStorable = false };
				setSubtitlesFont = new JSONStorableStringChooser("Change subtitles font", fontChoices, "Arial", "Change subtitles font", choice => SubtitlesFontCallback(choice.val)){ isStorable = false };
				setSubtitlesAlignment = new JSONStorableStringChooser("Change subtitles alignment", alignmentChoices, "Bottom", "Change subtitles alignment", choice => SubtitlesAlignementCallback(choice.val)){ isStorable = false };
				showSubtitles5secs = new JSONStorableAction("Show subtitles for 5secs", () => { doShowSubtitles5secs(); });
				showSubtitlesPermanent = new JSONStorableAction("Show subtitles permanently", () => { doShowSubtitlesPermanent(); });
				hideSubtitles = new JSONStorableAction("Hide subtitles", () => { doHideSubtitles(); });
				
				// Fake actions to split things the user can use safely
				JSONStorableAction fakeFuncUseBelow = new JSONStorableAction("- - - - Use these functions below ↓ - - - - -", () => {});
				JSONStorableAction fakeFuncDoNotUseBelow = new JSONStorableAction("- - - - Avoid using these functions below ↓ - - - - -", () => {});
				
				// **********************************************************************************************************************************
				// Registering variables, a bit strange to disconnect them from the initial creation, but allows me to order them in the action list
				// **********************************************************************************************************************************
				RegisterAction(fakeFuncUseBelow);
				RegisterAction(triggerFadeIn);
				RegisterAction(triggerFadeOut);
				RegisterColor(setFadeColor);
				RegisterFloat(setFadeInTime);
				RegisterFloat(setFadeOutTime);
				
				RegisterString(SubtitlesText);
				RegisterColor(setSubtitlesColor);
				RegisterFloat(setSubtitlesSize);
				RegisterStringChooser(setSubtitlesFont);
				RegisterStringChooser(setSubtitlesAlignment);
				RegisterAction(showSubtitles5secs);
				RegisterAction(showSubtitlesPermanent);
				RegisterAction(hideSubtitles);
				
				// JSONStorable Variables, the user should not use these without changing the save
				RegisterAction(fakeFuncDoNotUseBelow);
				RegisterColor(FadeColor);
				RegisterBool(FadeAtStart);
				RegisterFloat(FadeInTime);
				RegisterFloat(FadeOutTime);
				
				RegisterColor(SubtitlesColor);
				RegisterFloat(SubtitlesSize);
				RegisterStringChooser(SubtitlesFontChoice);
				RegisterStringChooser(SubtitleAlignmentChoice);
				
				// Settings "current" variables
				CurrentFadeColor = FadeColor.colorPicker.currentColor;
				CurrentSubtitlesColor = SubtitlesColor.colorPicker.currentColor;
				CurrentFadeInTime = FadeInTime.val;
				CurrentFadeOutTime = FadeOutTime.val;
				
				// Initializing my VR flag (maybe at some point I'll need to make some different checks, so I'm making that in advance)
				isPlayerInVr = XRDevice.isPresent;
	
			}
            catch(Exception e)
            {
                SuperController.LogError("VAMOverlays - Exception caught: " + e);
            }
		}
		
		void Start()
        {
			try
            {
				InitFadeObjects();
				// If we fade after scene load
				if( FadeAtStart.val == true ) {
					// Make the fade layer opaque
					FadeImg.canvasRenderer.SetAlpha(1.0f);
				}
			}
            catch(Exception e)
            {
                SuperController.LogError("VAMOverlays - Exception caught: " + e);
            }
        }
		
		void Update() {
			updateDebugArea();
		}
		
		// **************************
		// Functions
		// **************************
		
		protected void updateDebugArea() {
			if( showDebug == true ) {
				debugArea.val = "<b>DEBUG</b>\n"
				+ "VR enabled : " + ( isPlayerInVr == true ? "yes" : "no" ) + " \n"
				+ "";				
			}
		}
		
		protected string getRandomQuote() {
			var random = new System.Random();
			int quoteIndex = random.Next(SubtitlesQuotes.Count);
			return SubtitlesQuotes[quoteIndex];
		}
		
		// Global FadeIn Function
		protected void FadeIn() {
			FadeImg.canvasRenderer.SetAlpha(1.0f);
			FadeImg.CrossFadeAlpha(0.0f,CurrentFadeInTime,false);
		}
		
		// Triggers the FadeIn when you click on the test button
		protected void TestFadeIn() {
			// Make the fade layer opaque
			FadeImg.canvasRenderer.SetAlpha(1.0f);
			FadeImg.CrossFadeAlpha(0.0f,FadeInTime.val,false);
		}
		
		// Global FadeOut Function
		protected void FadeOut() {
			FadeImg.canvasRenderer.SetAlpha(0.0f);
			FadeImg.CrossFadeAlpha(1.0f,CurrentFadeOutTime,false);
		}
		
		// Triggers the FadeOut when you click on the test button
		protected void TestFadeOut() {
			FadeImg.canvasRenderer.SetAlpha(0.0f);
			FadeImg.CrossFadeAlpha(1.0f,FadeOutTime.val,false);
			Invoke("FadeIn", FadeOutTime.val + 5.0f);
		}
		
		protected void ChangeSubtitlesFont( string fontVal ) {
			if( SubtitlesTxt != null ) {
				SubtitlesTxt.font = FontAssets[fontVal];
			}
		}
		
		protected void ChangeSubtitlesAlignment( string alignmentVal ) {
			if( SubtitlesTxt != null ) {
				if( alignmentVal == "Center" ) {
					SubtitlesTxt.alignment = TextAnchor.MiddleCenter;
				} else if( alignmentVal == "Top" ) {
					SubtitlesTxt.alignment = TextAnchor.UpperCenter;
				} else {					
					SubtitlesTxt.alignment = TextAnchor.LowerCenter;
				}
			}
		}
		
		protected int getFontSize( float size ) {
			float finalSize = size * 2.34f; // original value * tweak for updates (to avoid breaking old scenes)
			if( isPlayerInVr ) {
				finalSize = size * 5f; // VR multiplier - was 2.5f
			}
			return (int)Math.Round(finalSize);
		}
		
		protected void InitFadeObjects() {
			try
            {
				// ******************************
				// CREATION OF THE ELEMENTS
				// ******************************
				// Creation of the main Canvas
				VAMOverlaysGO = new GameObject();
				VAMOverlaysGO.name = "FadeCanvas";
				VAMOverlaysGO.transform.SetParent(Camera.main.transform);
				VAMOverlaysGO.transform.localRotation = Quaternion.identity;
				VAMOverlaysGO.transform.localPosition = new Vector3(0, 0, 0);
				VAMOverlaysGO.layer = 5;
				OverlaysCanvas = VAMOverlaysGO.AddComponent<Canvas>();
				OverlaysCanvas.renderMode = RenderMode.WorldSpace;
				OverlaysCanvas.sortingOrder = 2;
				OverlaysCanvas.worldCamera = Camera.main;
				OverlaysCanvas.planeDistance = 10.0f;
				OverlaysCanvasScaler = VAMOverlaysGO.AddComponent<CanvasScaler>();

				// Configuration of the RectTransform of the Canvas
				RectTransform CanvasRecTr = VAMOverlaysGO.GetComponent<RectTransform>();
				CanvasRecTr.sizeDelta = new Vector2(2560f, 1440f);
				CanvasRecTr.localPosition = new Vector3(0,0,0.8f); // wav 0.3f initially
				CanvasRecTr.localScale = new Vector3(0.00024f, 0.00024f, 1.0f);

				// Creation of the structure for the fade
				FadeImgGO = new GameObject("FadeImage");
				FadeImgGO.layer = 5;
				FadeImgGO.transform.SetParent(VAMOverlaysGO.transform);

				FadeImgGO.AddComponent<CanvasRenderer>();
				FadeImg = FadeImgGO.AddComponent<Image>();
				FadeImg.raycastTarget = false;
				FadeImg.canvasRenderer.SetAlpha(0.0f);
				FadeImg.color = CurrentFadeColor;

				// Configuration of the RectTransform of the fade
				RectTransform FadeImgRecTr = FadeImg.GetComponent<RectTransform>();

				FadeImgRecTr.localPosition = new Vector3(0, 0, 0);
				FadeImgRecTr.localScale = new Vector3(1, 1, 1);
				FadeImgRecTr.localRotation = Quaternion.identity;
				FadeImgRecTr.anchorMin = new Vector2(0, 0);
				FadeImgRecTr.anchorMax = new Vector2(1, 1);
				FadeImgRecTr.offsetMin = new Vector2(-4000.0f, -4000.0f);
				FadeImgRecTr.offsetMax = new Vector2(4000.0f, 4000.0f);

				// Creation of the structure for subtitles
				SubtitlesTxtGO = new GameObject("SubtitlesText");
				SubtitlesTxtGO.layer = 5;
				SubtitlesTxtGO.transform.SetParent(VAMOverlaysGO.transform);

				SubtitlesTxtGO.AddComponent<CanvasRenderer>();
				SubtitlesTxt = SubtitlesTxtGO.AddComponent<Text>();
				SubtitlesTxtShadow = SubtitlesTxtGO.AddComponent<Shadow>();

				// Defining text properties
				SubtitlesTxt.raycastTarget = false;
				SubtitlesTxt.canvasRenderer.SetAlpha(0.0f);
				SubtitlesTxt.color = CurrentSubtitlesColor;
				SubtitlesTxt.text = getRandomQuote();
				
				SubtitlesTxt.font = FontAssets["Arial"];
				
				SubtitlesTxt.fontSize = getFontSize( 18 ); // Will deal automatically with the ratio depending on the VR or desktop state - Was 18 initially

				// Selecting the default alignment
				ChangeSubtitlesAlignment( SubtitleAlignmentChoice.val );			

				// Defining shadow properties
				SubtitlesTxtShadow.effectColor = Color.black;
				SubtitlesTxtShadow.effectDistance = new Vector2(2f, -0.5f);

				// And finally again, RectTransform tweaks
				RectTransform SubtitlesRecTr = SubtitlesTxt.GetComponent<RectTransform>();
				SubtitlesRecTr.localPosition = new Vector3(0, 0, 0);
				SubtitlesRecTr.localScale = new Vector3(1, 1, 1);
				SubtitlesRecTr.localRotation = Quaternion.identity;
				SubtitlesRecTr.anchorMin = new Vector2(0, 0);
				SubtitlesRecTr.anchorMax = new Vector2(1, 1);
				
				// **********************************
				// SETUP BASED ON VR OR DESKTOP MODE
				// **********************************
				
				// VR Configs
				if( isPlayerInVr == true ) {
					SubtitlesRecTr.offsetMin = new Vector2(370.0f, 0.0f); // Was 280f initially
					SubtitlesRecTr.offsetMax = new Vector2(-370.0f, 0.0f);
				// Desktop configs
				} else {
					SubtitlesRecTr.offsetMin = new Vector2(300.0f, -200.0f);
					SubtitlesRecTr.offsetMax = new Vector2(-300.0f, 200.0f);
				}
			}
            catch(Exception e)
            {
                SuperController.LogError("VAMOverlays - Exception caught will initializing: " + e);
            }
		}
		
		protected void doShowSubtitles5secs() {
			if ( SubtitlesTxt != null ) {
				SubtitlesTxt.canvasRenderer.SetAlpha(0.0f);
				SubtitlesTxt.CrossFadeAlpha(1.0f, 1.0f,false);
				Invoke("doHideSubtitles", 6.0f );
			}
		}
		
		protected void doShowSubtitlesPermanent() {
			if ( SubtitlesTxt != null ) {
				SubtitlesTxt.canvasRenderer.SetAlpha(0.0f);
				SubtitlesTxt.CrossFadeAlpha(1.0f, 1.0f,false);
			}
		}
		
		protected void doHideSubtitles() {
			if ( SubtitlesTxt != null ) {
				SubtitlesTxt.CrossFadeAlpha(0.0f, 1.0f,false);
			}
		}

		// **************************
		// Callbacks
		// **************************
		private void OnFontsBundleLoaded(Request aRequest) {
			// Loading font assets
			foreach( KeyValuePair<string, string> fontKvp in FontList )
			{
				Font fnt = aRequest.assetBundle.LoadAsset<Font>(fontKvp.Value);
				if( fnt != null ) {
					// Adding font asset if successfuly loaded in the assets
					FontAssets.Add(fontKvp.Key, fnt);
					// If we have a value set in the custom UI that is the same as our font, let's update the Canvas
					if( SubtitleAlignmentChoice.val == fontKvp.Key ) {
						ChangeSubtitlesFont(fontKvp.Key);
					}
				}
			}	

		}
		
		protected void FadeColorCallback(JSONStorableColor selectedColor) {
			if ( FadeImg != null ) {
				FadeImg.color = HSVColorPicker.HSVToRGB(selectedColor.val);
				CurrentFadeColor = HSVColorPicker.HSVToRGB(selectedColor.val);
			}
		}
		
		protected void setFadeInTimeCallback(float fadeintime) {
			CurrentFadeInTime = fadeintime;
		}
		
		protected void setFadeOutTimeCallback(float fadeouttime) {
			CurrentFadeOutTime = fadeouttime;
		}
		
		protected void SubtitlesColorCallback(JSONStorableColor selectedColor) {
			if ( SubtitlesTxt != null ) {
				SubtitlesTxt.color = HSVColorPicker.HSVToRGB(selectedColor.val);
				CurrentSubtitlesColor = HSVColorPicker.HSVToRGB(selectedColor.val);
			}
		}
		
		protected void SetFadeColorCallback(JSONStorableColor val) {
			if ( FadeImg != null ) {
				FadeImg.color = val.colorPicker.currentColor;
				CurrentFadeColor = val.colorPicker.currentColor;
			}
		}
		
		protected void PreviewSubtitlesCallback(JSONStorableBool state) {
			if ( SubtitlesTxt != null ) {
				if( state.val == true ) {
					SubtitlesTxt.text = getRandomQuote();
					SubtitlesTxt.canvasRenderer.SetAlpha(1.0f);
				} else {
					SubtitlesTxt.canvasRenderer.SetAlpha(0.0f);
				}
			}
		}
		
		protected void SubtitlesValueCallback(JSONStorableString text) {
			if ( SubtitlesTxt != null ) {
				SubtitlesTxt.text = SubtitlesText.val;
			}
		}
		
		protected void SubtitlesSizeCallback(float subtitlesSize) {
			if( SubtitlesTxt != null ) {
				SubtitlesTxt.fontSize = getFontSize( subtitlesSize );
			}
		}
		
		protected void SubtitlesFontCallback(string fontchoice) {
			ChangeSubtitlesFont( fontchoice );
		}
		protected void SubtitlesAlignementCallback(string alignementchoice) {
			ChangeSubtitlesAlignment( alignementchoice );
		}
			
		// **************************
		// Local Tools
		// **************************
		private void logDebug( string debugText ) {
			SuperController.LogMessage( debugText );
		}
		
		// **************************
		// Time to cleanup !
		// **************************
		void OnDestroy() {
			// Removing the main object, every children will be destroyed too
			Destroy(VAMOverlaysGO);
			
			// Font bundle unload
			AssetLoader.DoneWithAssetBundleFromFile(FONTS_BUNDLE_PATH);
		}
		
		// ***********************************************************
		// EXTERNAL TOOL - Thank you great coders for your content!
		// ***********************************************************
		
		// *********** MacGruber_Utils.cs START *********************
		// Get directory path where the plugin is located. Based on Alazi's & VAMDeluxe's method.
		public static string GetPluginPath(MVRScript self)
		{
			string id = self.name.Substring(0, self.name.IndexOf('_'));
			string filename = self.manager.GetJSON()["plugins"][id].Value;
			return filename.Substring(0, filename.LastIndexOfAny(new char[] { '/', '\\' }));
		}
				
		// Get path prefix of the package that contains our plugin.
		public static string GetPackagePath(MVRScript self)
		{
			string id = self.name.Substring(0, self.name.IndexOf('_'));
			string filename = self.manager.GetJSON()["plugins"][id].Value;
			int idx = filename.IndexOf(":/");
			if (idx >= 0)
				return filename.Substring(0, idx+2);
			else
				return string.Empty;
		}
				
		// Check if our plugin is running from inside a package
		public static bool IsInPackage(MVRScript self)
		{
			string id = self.name.Substring(0, self.name.IndexOf('_'));
			string filename = self.manager.GetJSON()["plugins"][id].Value;
			return filename.IndexOf(":/") >= 0;
		}
		// *********** MacGruber_Utils.cs END *********************
	}
}