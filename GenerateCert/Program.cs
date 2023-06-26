using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace GenerateCert
{
	internal class Program
	{
		static void Main ( string [] args )
		{
			var certificateName = "Blazor Service";
			var password = "1234";

			var cert = CreateCertificate ( certificateName, password );

			// Create PFX (PKCS #12) with private key
			File.WriteAllBytes ( "c:\\temp\\blzcert.pfx", cert.Export ( X509ContentType.Pfx, password ) );


			// Create Base 64 encoded CER (public key only)
			File.WriteAllText ( 

				"c:\\temp\\blzcert.cer",
				"-----BEGIN CERTIFICATE-----\r\n"
				+ Convert.ToBase64String ( cert.Export ( X509ContentType.Cert ), Base64FormattingOptions.InsertLineBreaks )
				+ "\r\n-----END CERTIFICATE-----" );


		}


		private static X509Certificate2 CreateCertificate ( string certificateName, string password )
		{

			var issuedBy = "Microsoft Enhanced RSA and AES Cryptographic Provider";

			SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder ();
			sanBuilder.AddIpAddress ( IPAddress.Loopback );
			sanBuilder.AddIpAddress ( IPAddress.IPv6Loopback );

			X500DistinguishedName distinguishedName = new X500DistinguishedName ( $"CN={certificateName}" );


			var issuedTo = "GAGEtrak Web API Server";

			using ( RSA rsa = new RSACryptoServiceProvider ( 2048 * 2, new CspParameters ( 24, issuedBy, issuedTo ) ) )
			{

				var request = new CertificateRequest ( distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1 );

				request.CertificateExtensions.Add ( sanBuilder.Build () );

				var certificate = request.CreateSelfSigned ( new DateTimeOffset ( DateTime.UtcNow.AddDays ( -1 ) ), new DateTimeOffset ( DateTime.UtcNow.AddDays ( 3650 ) ) );
				bool isWindows = System.Runtime.InteropServices.RuntimeInformation
							  .IsOSPlatform ( OSPlatform.Windows );
				if ( isWindows )
					certificate.FriendlyName = certificateName;

				return certificate;


				// return new X509Certificate2(certificate.Export(X509ContentType.Pfx, password), password, X509KeyStorageFlags.MachineKeySet);
			}
		}

	}

}
