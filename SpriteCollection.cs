using Godot;
public partial class SpriteCollection : Node {
    [Export]
    public CompressedTexture2D[] AlbedoTextures { get; set; } = [];

    [Export]
    public CompressedTexture2D[] NormalTextures { get; set; } = [];

    [Export]
    public CompressedTexture2D[] OcclusionTextures { get; set; } = [];

    [Export]
    public BaseMaterial3D.BillboardModeEnum Billboard { get; set; } = BaseMaterial3D.BillboardModeEnum.Enabled;

    [Export]
    public BaseMaterial3D.TextureFilterEnum TextureFilter { get; set; } = BaseMaterial3D.TextureFilterEnum.NearestWithMipmapsAnisotropic;

    [Export]
    public float AnimationSpeedScale { get; set; } = 5.0f;
}
