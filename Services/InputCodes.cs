namespace KeysVisorOverlay.Services;

public static class InputCodes
{
    public const int MouseLeft = 0x1001;
    public const int MouseRight = 0x1002;
    public const int MouseMiddle = 0x1003;
    public const int MouseX1 = 0x1004;
    public const int MouseX2 = 0x1005;

    public const int VkControl = 0x11;
    public const int VkShift = 0x10;
    public const int VkLeftControl = 0xA2;
    public const int VkRightControl = 0xA3;
    public const int VkLeftShift = 0xA0;
    public const int VkRightShift = 0xA1;
    public const int VkH = 0x48;
    public const int VkQ = 0x51;
    public const int VkR = 0x52;
    public const int VkSpace = 0x20;
    public const int VkZ = 0x5A;
    public const int VkX = 0x58;

    public static string GetDisplayName(int code)
    {
        return code switch
        {
            MouseLeft => "M1",
            MouseRight => "M2",
            MouseMiddle => "M3",
            MouseX1 => "M4",
            MouseX2 => "M5",
            VkSpace => "SPACE",
            VkZ => "Z",
            VkX => "X",
            _ when code >= 'A' && code <= 'Z' => ((char)code).ToString(),
            _ when code >= '0' && code <= '9' => ((char)code).ToString(),
            _ => $"VK {code}"
        };
    }

    public static bool IsControl(int code)
    {
        return code is VkControl or VkLeftControl or VkRightControl;
    }

    public static bool IsShift(int code)
    {
        return code is VkShift or VkLeftShift or VkRightShift;
    }
}
