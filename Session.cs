namespace Gelir_Gider_Projesi
{
    // Basit oturum/transfer sýnýfý: kayýt sýrasýnda dönen yeni kullanýcý id'sini
    // geçici olarak taþýmak için kullanýlýr.
    // Not: Bu sadece uygulama çalýþýrken geçicidir. Güvenlik için gerçek kimlik doðrulama
    // her zaman giriþ sýrasýnda veritabanýndan yapýlmalýdýr.
    public static class Session
    {
        public static int PendingRegisteredUserId { get; set; } = 0;
        public static string PendingRegisteredEmail { get; set; } = null;
    }
}
