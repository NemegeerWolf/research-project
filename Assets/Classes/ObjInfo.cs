using Newtonsoft.Json;
using System;

/// <summary>
/// Summary description for Class1
/// </summary>



public class ObjInfo
{



	[JsonProperty]
	internal string id;
	[JsonProperty]
	internal float scale;
    [JsonProperty]
    internal float colliderPrecision;

}
