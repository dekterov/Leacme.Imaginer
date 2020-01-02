// Copyright (c) 2017 Leacme (http://leac.me). View LICENSE.md for more information.
using Godot;
using System;
using System.Threading.Tasks;

public class Hud : Node2D {

	private TextureRect vignette = new TextureRect() {
		Expand = true,
		Texture = new GradientTexture() {
			Gradient = new Gradient() { Colors = new[] { Colors.Transparent } }
		},
		Material = new ShaderMaterial() {
			Shader = new Shader() {
				Code = @"
					shader_type canvas_item;
					void fragment() {
						float iRad = 0.3;
						float oRad = 1.0;
						float opac = 0.5;
						vec2 uv = SCREEN_UV;
					    vec2 cent = uv - vec2(0.5);
					    vec4 tex = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0.0);
					    vec4 col = vec4(1.0);
					    col.rgb *= 1.0 - smoothstep(iRad, oRad, length(cent));
					    col *= tex;
					    col = mix(tex, col, opac);
					    COLOR = col;
					}"
			}
		}
	};

	private FileDialog filePopup = new FileDialog();
	private PopupPanel errorDiag = new PopupPanel();
	private Label errorLabel = new Label() { Text = "Could not display file.", Align = Label.AlignEnum.Center };
	private Sprite imageSprite = new Sprite();

	public override void _Ready() {
		InitVignette();

		var btHolder = new VBoxContainer() { MarginTop = 20, RectMinSize = GetViewportRect().Size };
		AddChild(btHolder);

		Button openObjBt = new Button();
		openObjBt.Text = "Open Image";
		openObjBt.Connect("pressed", this, nameof(OnOpenButton));
		openObjBt.SizeFlagsHorizontal = (int)Control.SizeFlags.ShrinkCenter;
		openObjBt.RectMinSize = new Vector2(btHolder.RectMinSize.x * 0.7f, 40);
		openObjBt.AddFontOverride("font", new DynamicFont() { FontData = GD.Load<DynamicFontData>("res://assets/default/Tuffy_Bold.ttf"), Size = 30 });
		btHolder.AddChild(openObjBt);

		filePopup.Connect("file_selected", this, nameof(OnFileSelected));
		AddChild(filePopup);

		imageSprite = new Sprite();
		AddChild(imageSprite);

		var labHolder = new VBoxContainer() { Alignment = BoxContainer.AlignMode.Center };
		AddChild(errorDiag);
		errorDiag.AddChild(labHolder);
		labHolder.AddChild(errorLabel);
	}

	private void OnFileSelected(String path) {
		RemoveChild(imageSprite);
		imageSprite = new Sprite();
		AddChild(imageSprite);

		string imgType = null;
		switch (System.IO.Path.GetExtension(path).ToLower()) {
			case (".png"): imgType = ".png"; break;
			case (".jpeg"):
			case (".jpg"): imgType = ".jpg"; break;
			case (".webp"): imgType = ".webp"; break;
		}

		if (imgType == null) {
			errorDiag.PopupCenteredRatio();
			errorDiag.RectSize = new Vector2(errorDiag.RectSize.x, errorLabel.RectSize.y);
			errorDiag.RectPosition = new Vector2(errorDiag.RectPosition.x, GetViewportRect().Size.y / 2f);
			return;
		}

		Task.Run(() => {
			try {
				var imageFile = new File();
				imageFile.Open(path, File.ModeFlags.Read);
				var buffer = imageFile.GetBuffer((int)imageFile.GetLen());
				var image = new Image();
				if (imgType.Equals(".png")) {
					image.LoadPngFromBuffer(buffer);
				} else if (imgType.Equals(".jpeg") || imgType.Equals(".jpg")) {
					image.LoadJpgFromBuffer(buffer);
				} else if (imgType.Equals(".webp")) {
					image.LoadWebpFromBuffer(buffer);
				}
				var im = new ImageTexture();
				im.CreateFromImage(image);
				imageSprite.Texture = im;
				var xScale = GetViewportRect().Size.x / im.GetSize().x;
				imageSprite.Scale = new Vector2(xScale * 0.95f, xScale * 0.95f);
				imageSprite.Translate(new Vector2(GetViewportRect().Size.x / 2f, GetViewportRect().Size.y / 2f));
			} catch {
				errorDiag.PopupCenteredRatio();
				errorDiag.RectSize = new Vector2(errorDiag.RectSize.x, errorLabel.RectSize.y);
				errorDiag.RectPosition = new Vector2(errorDiag.RectPosition.x, GetViewportRect().Size.y / 2f);
			}
		});

	}

	private void OnOpenButton() {
		filePopup.PopupExclusive = true;
		filePopup.PopupCenteredRatio();
		filePopup.Access = FileDialog.AccessEnum.Filesystem;
		filePopup.Mode = FileDialog.ModeEnum.OpenFile;
		filePopup.Filters = new[] { "*.png; PNG Image", "*.jpeg; JPEG Image", "*.jpg; JPG Image", "*.webp; WEBP Image" };
		filePopup.CurrentPath = OS.GetSystemDir(Godot.OS.SystemDir.Pictures) + "/";
	}

	public override void _Draw() {
		DrawBorder(this);
	}

	private void InitVignette() {
		vignette.RectMinSize = GetViewportRect().Size;
		AddChild(vignette);
		if (Lib.Node.VignetteEnabled) {
			vignette.Show();
		} else {
			vignette.Hide();
		}
	}

	public static void DrawBorder(CanvasItem canvas) {
		if (Lib.Node.BoderEnabled) {
			var vps = canvas.GetViewportRect().Size;
			int thickness = 4;
			var color = new Color(Lib.Node.BorderColorHtmlCode);
			canvas.DrawLine(new Vector2(0, 1), new Vector2(vps.x, 1), color, thickness);
			canvas.DrawLine(new Vector2(1, 0), new Vector2(1, vps.y), color, thickness);
			canvas.DrawLine(new Vector2(vps.x - 1, vps.y), new Vector2(vps.x - 1, 1), color, thickness);
			canvas.DrawLine(new Vector2(vps.x, vps.y - 1), new Vector2(1, vps.y - 1), color, thickness);
		}
	}
}
