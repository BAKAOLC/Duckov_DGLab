namespace Duckov_DGLab.Configs
{
    public class DGLabConfig : ConfigBase
    {
        public int HurtDuration { get; set; } = 1;
        public string? HurtWaveType { get; set; }
        public int DeathDuration { get; set; } = 3;
        public string? DeathWaveType { get; set; }
        public int DefaultStrength { get; set; }

        public override void LoadDefault()
        {
            HurtDuration = 1;
            HurtWaveType = null;
            DeathDuration = 3;
            DeathWaveType = null;
            DefaultStrength = 0;
        }

        // ReSharper disable InvertIf
        public override bool Validate()
        {
            var changed = false;
            if (HurtDuration is < 1 or > 5)
            {
                ModLogger.LogWarning($"Invalid HurtDuration: {HurtDuration}, resetting to default (1)");
                HurtDuration = 1;
                changed = true;
            }

            if (DeathDuration is < 1 or > 5)
            {
                ModLogger.LogWarning($"Invalid DeathDuration: {DeathDuration}, resetting to default (3)");
                DeathDuration = 3;
                changed = true;
            }

            if (DefaultStrength is < 0 or > 100)
            {
                ModLogger.LogWarning($"Invalid DefaultStrength: {DefaultStrength}, resetting to default (0)");
                DefaultStrength = 0;
                changed = true;
            }

            return changed;
        }
        // ReSharper restore InvertIf

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not DGLabConfig otherConfig) return;
            HurtDuration = otherConfig.HurtDuration;
            HurtWaveType = otherConfig.HurtWaveType;
            DeathDuration = otherConfig.DeathDuration;
            DeathWaveType = otherConfig.DeathWaveType;
            DefaultStrength = otherConfig.DefaultStrength;
        }
    }
}