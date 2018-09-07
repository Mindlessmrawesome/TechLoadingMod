using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using UnityEngine;
using System.Reflection;

namespace TechLoadingMod
{


    class QPatch
    {
        public static void Main()

        {
            new GameObject().AddComponent<ExecuteButton>();
            var harmony = HarmonyInstance.Create("mindless.ttmm.techloading.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    public class Class1
    {

    }
    public class ExecuteButton : MonoBehaviour
    {

        void OnGUI()
        {
            if (GUI.Button(new Rect(25, 25, 100, 30), "Save current tech as a game-spawning tech"))
            {

            }
        }
    }
}