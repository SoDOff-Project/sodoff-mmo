using System;
using HarmonyLib;

namespace SoDOff_MMO_Plugin {
	// all classes from SoDOff_MMO_Plugin namespace will be created on dll load
	public class SamplePlugin1 {
		public SamplePlugin1() {
			Console.WriteLine("SamplePlugin1 constructor");
			var harmony = new Harmony("SamplePlugin1");
			harmony.PatchAll();
		}
	}
	public class SamplePlugin2 {
		public SamplePlugin2() {
			Console.WriteLine("SamplePlugin2 constructor");
		}
	}
}

namespace Internal {
	// classes from other namespace will be NOT created on dll load
	public class SamplePlugin3 {
		public SamplePlugin3() {
			Console.WriteLine("SamplePlugin3 constructor");
		}
	}
}


[HarmonyPatch("ChatMessageHandler", "Chat")]
public class ChatMessageHandler_Chat {
    [HarmonyPrefix]
    public static void Chat(sodoffmmo.Core.Client client, string message) {
        Console.WriteLine($"Chat message: {message}");
    }
}
