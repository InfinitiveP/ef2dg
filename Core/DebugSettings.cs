namespace ef2dg
{
    public static class DebugSettings
    {
        public static bool ShowPlayerHitbox = false;
        public static bool ShowEnemyHitbox = false;
        public static bool ShowMapHitbox = false;
        public static bool ShowAttackZone = false;
        public static bool ShowPerformanceMonitor = true;

        public static bool IsEditorMode = true;

        // Показывать освещение сцены в режиме редактирования (в Play Mode оно включено всегда)
        public static bool PreviewLightingInEditor = true;
    }
}