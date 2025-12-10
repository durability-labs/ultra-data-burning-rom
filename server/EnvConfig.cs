namespace UltraDataBurningROM.Server
{
    public static class EnvConfig
    {
        public static ulong VolumeSize { get; private set; }
        public static string[] KnownUsers { get; private set; }

        static EnvConfig()
        {
            VolumeSize = Convert.ToUInt64(Get("BROM_ROMVOLUMESIZE"));
            KnownUsers = Get("BROM_USERNAMES").Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }

        private static string Get(string name)
        {
            var envVar = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(envVar)) throw new Exception("Missing environment variable: " + name);
            return envVar;
        }
    }
}
