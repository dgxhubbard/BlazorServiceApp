using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorServiceApp
{
	public  class Ports
	{
		#region Constants

		public const string PortsFile = "ports.json";
		public const int DefaultIdpPort = 7296;
		public const int DefaultApiPort = 7224;


		#endregion

		#region Properties

		public int IdpPort
		{  get; set; }


		public int ApiPort
		{ get; set; }

		#endregion


		#region Methods

		public static bool Exists ( string filepath )
		{
			var exists = false;

			if ( string.IsNullOrEmpty ( filepath ) )
				throw new ArgumentNullException ( "filepath required" );

			if ( !Directory.Exists ( filepath ) )
				throw new InvalidOperationException ( "filepath does not exist " + filepath );

			var path = Path.Combine ( filepath, PortsFile );

			if ( File.Exists ( path ) )
				exists = true;

			return exists;

		}

		public static Ports LoadPorts ( string filepath )
		{

			if ( string.IsNullOrEmpty ( filepath ) )
				throw new ArgumentNullException ( "filepath required" );

			if ( !Directory.Exists ( filepath ) )
				throw new InvalidOperationException ( "filepath does not exist " + filepath );

			var path = Path.Combine ( filepath, PortsFile );

			var json = File.ReadAllText ( path );

			var resp = new Ports ();
			resp = JsonSerializer.Deserialize<Ports> ( json );


			return resp;

		}

		public static void SavePorts ( string filepath )
		{
			if ( string.IsNullOrEmpty ( filepath ) )
				throw new ArgumentNullException ( "filepath required" );

			if ( !Directory.Exists ( filepath ) )
				throw new InvalidOperationException ( "filepath does not exist " +  filepath );


			var bldr = new StringBuilder ();

			bldr.AppendLine ( "{" );
			bldr.AppendLine ( string.Format ( "\"IdpPort\":{0},", DefaultIdpPort ) );
			bldr.AppendLine ( string.Format ( "\"ApiPort\":{0}", DefaultApiPort ) );
			bldr.AppendLine ( "}" );

			var jsonText = bldr.ToString ();

			var path = Path.Combine ( filepath, PortsFile );
			File.WriteAllText ( path, jsonText );

		}



		#endregion





	}
}
