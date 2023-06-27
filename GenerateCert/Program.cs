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

        /// <summary>
        /// Refer to:
        /// https://www.humankode.com/asp-net-core/develop-locally-with-https-self-signed-certificates-and-asp-net-core/
		/// https://stackoverflow.com/questions/42786986/how-to-create-a-valid-self-signed-x509certificate2-programmatically-not-loadin/50138133#50138133
		/// https://learn.microsoft.com/en-us/powershell/module/pki/new-selfsignedcertificate?view=windowsserver2022-ps
		/// 
        /// </summary>


        private static X509Certificate2 CreateCertificate ( string certificateName, string password )
		{

			var issuedBy = "Microsoft Enhanced RSA and AES Cryptographic Provider";

			var sanBuilder = new SubjectAlternativeNameBuilder ();

			sanBuilder.AddIpAddress ( IPAddress.Loopback );
			sanBuilder.AddIpAddress ( IPAddress.IPv6Loopback );
            sanBuilder.AddDnsName ( "localhost" );
            sanBuilder.AddDnsName ( Environment.MachineName );

            var distinguishedName = new X500DistinguishedName ( $"CN={certificateName}" );


			var issuedTo = "Blazor App Service";

			using ( RSA rsa = new RSACryptoServiceProvider ( 2048 * 2, new CspParameters ( 24, issuedBy, issuedTo ) ) )
			{

				var request = 
					new CertificateRequest ( 
						distinguishedName, rsa, 
						HashAlgorithmName.SHA256, 
						RSASignaturePadding.Pkcs1 );

				request.CertificateExtensions.Add ( sanBuilder.Build () );


                request.CertificateExtensions.Add (
                    new X509KeyUsageExtension ( 
						X509KeyUsageFlags.DataEncipherment | 
						X509KeyUsageFlags.KeyEncipherment | 
						X509KeyUsageFlags.DigitalSignature, false ) );


                request.CertificateExtensions.Add (
                   new X509EnhancedKeyUsageExtension (
                       new OidCollection { new Oid ( "1.3.6.1.5.5.7.3.1" ) }, false ) );


                var certificate = 
					request.CreateSelfSigned ( 
						new DateTimeOffset ( 
							DateTime.UtcNow.AddDays ( -1 ) ), 
						new DateTimeOffset ( DateTime.UtcNow.AddDays ( 3650 ) ) );


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
