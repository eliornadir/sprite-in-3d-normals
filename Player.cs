using System.Linq;
using Godot;

public partial class Player : CharacterBody3D {
	[Export]
	public int WalkSpeed { get; set; } = 5;

	[Export]
	public float CollisionDepth { get; set; } = 0.8f;

	[ExportGroup("Sprite")]
	[Export]
	public MeshInstance3D SpriteMeshInstance { get; set; } = null!;

	[Export]
	public AnimationPlayer SpriteAnimationPlayer { get; set; } = null!;

	[Export]
	public CollisionShape3D SpriteCollisionShape { get; set; } = null!;

	[Export]
	public Godot.Collections.Array<SpriteCollection> Sprites { get; set; } = [];

	private SpriteCollection _currentSpriteCollection = null!;
	private CompressedTexture2D[] _spriteAlbedoTextures => _currentSpriteCollection.AlbedoTextures;
	private CompressedTexture2D[] _spriteNormalTextures => _currentSpriteCollection.NormalTextures;
	private CompressedTexture2D[] _spriteOcclusionTextures => _currentSpriteCollection.OcclusionTextures;
	private BaseMaterial3D.BillboardModeEnum _spriteBillboard => _currentSpriteCollection.Billboard;
	private BaseMaterial3D.TextureFilterEnum _spriteTextureFilter => _currentSpriteCollection.TextureFilter;
	private float _spriteAnimationSpeedScale => _currentSpriteCollection.AnimationSpeedScale;
	private Vector3 _targetVelocity = Vector3.Zero;

	public static class InputActions {
		public const string MoveUp = "PlayerMoveUp";
		public const string MoveLeft = "PlayerMoveLeft";
		public const string MoveDown = "PlayerMoveDown";
		public const string MoveRight = "PlayerMoveRight";
	}

	public override void _Ready() {
		if (Sprites.Count == 0 || Sprites[0] == null) {
			GD.PrintErr("No sprite collections provided.");
			return;
		}
		_currentSpriteCollection = Sprites[0];
		if (SpriteMeshInstance == null) {
			GD.PrintErr("SpriteMeshInstance is not assigned.");
			return;
		}
		if (SpriteAnimationPlayer == null) {
			GD.PrintErr("SpriteAnimationPlayer is not assigned.");
			return;
		}
		if (SpriteCollisionShape == null) {
			GD.PrintErr("SpriteCollisionShape is not assigned.");
			return;
		}
		LoadSprite();
	}

	public override void _PhysicsProcess(double delta) {
		Vector3 direction = Vector3.Zero;
		if (Input.IsActionPressed(InputActions.MoveUp)) {
			direction += Vector3.Forward;
		}
		if (Input.IsActionPressed(InputActions.MoveLeft)) {
			direction += Vector3.Left;
		}
		if (Input.IsActionPressed(InputActions.MoveDown)) {
			direction += Vector3.Back;
		}
		if (Input.IsActionPressed(InputActions.MoveRight)) {
			direction += Vector3.Right;
		}
		if (direction != Vector3.Zero) {
			ChangeSprite(1);
		}
		else {
			ChangeSprite(0);
		}
		_targetVelocity.X = direction.X * WalkSpeed;
		_targetVelocity.Z = direction.Z * WalkSpeed;
		Velocity = _targetVelocity;
		MoveAndSlide();
	}

	public void ChangeSprite(int index) {
		if (index < 0 || index >= Sprites.Count) {
			GD.PrintErr($"Invalid sprite collection index: {index}");
			return;
		}
		if (Sprites[index].Name == _currentSpriteCollection.Name) {
			return;
		}
		_currentSpriteCollection = Sprites[index];
		LoadSprite();
	}

	private void LoadSprite() {
		if (_spriteAlbedoTextures.Length == 0) {
			GD.PrintErr("No albedo textures provided.");
			return;
		}

		// Check if all albedo textures have the same size
		Vector2 albedoTextureSize = _spriteAlbedoTextures[0].GetSize();
		if (!_spriteAlbedoTextures.All(t => t.GetSize().IsEqualApprox(albedoTextureSize))) {
			GD.PrintErr("Albedo textures do not have a uniform size.");
			return;
		}

		// Create the material to be used for the quad
		StandardMaterial3D material = new() {
			Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			AlbedoTexture = _spriteAlbedoTextures[0],
			BillboardMode = _spriteBillboard,
			TextureFilter = _spriteTextureFilter
		};

		// If there are normal textures, check if they have the same size as the albedo textures
		// and set the first normal texture in the material
		if (_spriteNormalTextures.Length > 0) {
			if (!_spriteNormalTextures.All(t => t.GetSize().IsEqualApprox(albedoTextureSize))) {
				GD.PrintErr("Normal textures must have the same size as the albedo textures.");
				return;
			}
			material.NormalEnabled = true;
			material.NormalTexture = _spriteNormalTextures[0];
		}

		// If there are occlusion textures, check if they have the same size as the albedo textures
		// and set the first occlusion texture in the material
		if (_spriteOcclusionTextures.Length > 0) {
			if (!_spriteOcclusionTextures.All(t => t.GetSize().IsEqualApprox(albedoTextureSize))) {
				GD.PrintErr("Occlusion textures must have the same size as the albedo textures.");
				return;
			}
			material.AOEnabled = true;
			material.AOTexture = _spriteOcclusionTextures[0];
		}

		// Create the quad mesh and assign the material
		QuadMesh quadMesh = new() {
			Size = albedoTextureSize / Globals.World.PixelsPerMeter,
			Material = material
		};

		// Assign the quad mesh to the SpriteMeshInstance node
		SpriteMeshInstance.Mesh = quadMesh;

		// If there are multiple albedo textures, create an animation to cycle through them
		if (_spriteAlbedoTextures.Length > 1) {
			SpriteAnimationPlayer.SpeedScale = _spriteAnimationSpeedScale;

			Animation animation = new() {
				Length = _spriteAlbedoTextures.Length,
				LoopMode = Animation.LoopModeEnum.Linear
			};

			int albedoAnimationTrackIdx = animation.AddTrack(Animation.TrackType.Value);
			int normalAnimationTrackIdx = animation.AddTrack(Animation.TrackType.Value);
			int occlusionAnimationTrackIdx = animation.AddTrack(Animation.TrackType.Value);
			string spriteMeshName = SpriteMeshInstance.Name;
			animation.TrackSetPath(albedoAnimationTrackIdx, $"{spriteMeshName}:mesh:material:albedo_texture");
			if (_spriteNormalTextures.Length > 0) {
				animation.TrackSetPath(normalAnimationTrackIdx, $"{spriteMeshName}:mesh:material:normal_texture");
			}
			if (_spriteOcclusionTextures.Length > 0) {
				animation.TrackSetPath(occlusionAnimationTrackIdx, $"{spriteMeshName}:mesh:material:ao_texture");
			}
			for (int i = 0; i < _spriteAlbedoTextures.Length; i++) {
				// Add a track to change the albedo texture
				animation.TrackInsertKey(albedoAnimationTrackIdx, i, _spriteAlbedoTextures[i]);
				if (_spriteNormalTextures.Length > 0) {
					int ni = i <= _spriteNormalTextures.Length - 1 ? i : _spriteNormalTextures.Length - 1;
					animation.TrackInsertKey(normalAnimationTrackIdx, i, _spriteNormalTextures[ni]);
				}
				if (_spriteOcclusionTextures.Length > 0) {
					int oi = i <= _spriteOcclusionTextures.Length - 1 ? i : _spriteOcclusionTextures.Length - 1;
					animation.TrackInsertKey(occlusionAnimationTrackIdx, i, _spriteOcclusionTextures[oi]);
				}
			}

			if (!SpriteAnimationPlayer.HasAnimationLibrary(string.Empty)) {
				SpriteAnimationPlayer.AddAnimationLibrary(string.Empty, new AnimationLibrary());
			}
			// if (SpriteAnimationPlayer.HasAnimation("sprite_sequence")) {
			// 	SpriteAnimationPlayer.GetAnimationLibrary(string.Empty).RemoveAnimation("sprite_sequence");
			// }
			SpriteAnimationPlayer.GetAnimationLibrary(string.Empty).AddAnimation("sprite_sequence", animation);
			if (SpriteAnimationPlayer.HasAnimation("sprite_sequence")) {
				SpriteAnimationPlayer.Stop();
				SpriteAnimationPlayer.Play("sprite_sequence");
			}
		}
		
		Rect2I collisionShapeRect = _spriteAlbedoTextures[0].GetImage().GetUsedRect();
		GD.Print($"Collision shape rect: {collisionShapeRect}");
		BoxShape3D boxShape = new() {
			Size = new(collisionShapeRect.Size.Abs().X / Globals.World.PixelsPerMeter, collisionShapeRect.Size.Abs().Y / Globals.World.PixelsPerMeter, CollisionDepth)
		};
		SpriteCollisionShape.Shape = boxShape;
	}
}
