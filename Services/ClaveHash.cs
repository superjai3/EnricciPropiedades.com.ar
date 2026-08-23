using System.Security.Cryptography;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Hash de contraseñas con PBKDF2-SHA256. La contraseña en claro no se guarda
/// ni se registra en el log en ningún momento.
/// </summary>
public static class ClaveHash
{
    private const int TamanioSal = 16;
    private const int TamanioHash = 32;
    private const int Iteraciones = 210_000;

    public static (string Hash, string Sal) Calcular(string clave)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanioSal);
        return (Convert.ToBase64String(Derivar(clave, sal)), Convert.ToBase64String(sal));
    }

    /// <summary>Comparación en tiempo constante: no filtra información por el tiempo de respuesta.</summary>
    public static bool Verificar(string clave, string hashGuardado, string salGuardada)
    {
        byte[] sal;
        byte[] esperado;

        try
        {
            sal = Convert.FromBase64String(salGuardada);
            esperado = Convert.FromBase64String(hashGuardado);
        }
        catch (FormatException)
        {
            return false;
        }

        if (esperado.Length != TamanioHash)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Derivar(clave, sal), esperado);
    }

    private static byte[] Derivar(string clave, byte[] sal) =>
        Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, TamanioHash);
}
