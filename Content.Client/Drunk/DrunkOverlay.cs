using Content.Shared.Drunk;
using Content.Shared.StatusEffect;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Drunk;

public sealed class DrunkOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _drunkShader;

    public float CurrentBoozePower = 0.0f;

    private const float VisualThreshold = 10.0f;
    private const float PowerDivisor = 250.0f;

    private float _visualScale = 0;

    public DrunkOverlay()
    {
        IoCManager.InjectDependencies(this);
        _drunkShader = _prototypeManager.Index<ShaderPrototype>("Drunk").InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

        if (playerEntity == null)
            return;

        if (!_entityManager.HasComponent<DrunkComponent>(playerEntity)
            || !_entityManager.TryGetComponent<StatusEffectsComponent>(playerEntity, out var status))
            return;

        var statusSys = _sysMan.GetEntitySystem<StatusEffectsSystem>();
        if (!statusSys.TryGetTime(playerEntity.Value, SharedDrunkSystem.DrunkKey, out var time, status))
            return;

        var timeLeft = (float) (time.Value.Item2 - time.Value.Item1).TotalSeconds;
        CurrentBoozePower += (timeLeft - CurrentBoozePower) * args.DeltaSeconds / 16f;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalPlayer?.ControlledEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        _visualScale = BoozePowerToVisual(CurrentBoozePower);
        return _visualScale > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        _drunkShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _drunkShader.SetParameter("boozePower", _visualScale);
        handle.UseShader(_drunkShader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    /// <summary>
    ///     Converts the # of seconds the drunk effect lasts for (booze power) to a percentage
    ///     used by the actual shader.
    /// </summary>
    /// <param name="boozePower"></param>
    private float BoozePowerToVisual(float boozePower)
    {
        return Math.Clamp((boozePower - VisualThreshold) / PowerDivisor, 0.0f, 1.0f);
    }
}
