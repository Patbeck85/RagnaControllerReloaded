namespace RagnaController.Models
{
    public enum AdaptiveTriggerMode : byte
    {
        Off = 0x00,
        
        // Continuous resistance (Bow string)
        BowTension = 0x01,
        
        // Clicky resistance (Gun recoil / heavy melee)
        WeaponRecoil = 0x02,
        
        // Hard mechanical block (Stunned / Frozen status)
        HardBlock = 0x25,
        
        // Pulsing (Charging Magic Spells)
        MagicPulse = 0x06
    }
}
