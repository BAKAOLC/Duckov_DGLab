namespace Duckov_DGLab.Configs
{
    public class DGLabConfig : ConfigBase
    {
        public int HurtDuration { get; set; } = 1;
        public string? HurtWaveType { get; set; }
        public int DeathDuration { get; set; } = 3;
        public string? DeathWaveType { get; set; }
        public int DefaultStrength { get; set; }
        public int MaxStrength { get; set; } = 100;

        public override void LoadDefault()
        {
            HurtDuration = 1;
            HurtWaveType = null;
            DeathDuration = 3;
            DeathWaveType = null;
            DefaultStrength = 0;
            MaxStrength = 100;
        }

        public override bool Validate()
        {
            if (HurtDuration < 1 || HurtDuration > 5)
            {
                ModLogger.LogWarning($"Invalid HurtDuration: {HurtDuration}, resetting to default (1)");
                HurtDuration = 1;
            }

            if (DeathDuration < 1 || DeathDuration > 5)
            {
                ModLogger.LogWarning($"Invalid DeathDuration: {DeathDuration}, resetting to default (3)");
                DeathDuration = 3;
            }

            if (DefaultStrength < 0 || DefaultStrength > 100)
            {
                ModLogger.LogWarning($"Invalid DefaultStrength: {DefaultStrength}, resetting to default (0)");
                DefaultStrength = 0;
            }

            if (MaxStrength < 0 || MaxStrength > 100)
            {
                ModLogger.LogWarning($"Invalid MaxStrength: {MaxStrength}, resetting to default (100)");
                MaxStrength = 100;
            }

            return true;
        }

        public override void CopyFrom(IConfigBase other)
        {
            if (other is not DGLabConfig otherConfig) return;
            HurtDuration = otherConfig.HurtDuration;
            HurtWaveType = otherConfig.HurtWaveType;
            DeathDuration = otherConfig.DeathDuration;
            DeathWaveType = otherConfig.DeathWaveType;
            DefaultStrength = otherConfig.DefaultStrength;
            MaxStrength = otherConfig.MaxStrength;
        }
    }
}