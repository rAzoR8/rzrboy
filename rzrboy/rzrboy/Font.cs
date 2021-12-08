namespace rzrboy
{
    public static class Font
    {
        public const string Family = "Iosevka";
        public const string Regular = $"{Family}Regular";

        public const string Thin = $"{Family}Thing";
        public const string Light = $"{Family}Light";
        public const string Medium = $"{Family}Medium";
        public const string Bold = $"{Family}Bold";

        public static readonly string[] All = new []{ Thin, Light, Regular, Medium, Bold };
    }
}
