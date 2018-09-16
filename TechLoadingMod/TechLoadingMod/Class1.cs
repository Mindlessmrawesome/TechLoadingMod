using System;
using System.IO;
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
        public static string XMLPath; // a static variable called xmlpath for holding the tech folder's path

        public static void Main()
        {
            var harmony = HarmonyInstance.Create("mindless.ttmm.techloading.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            XMLPath = Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location, "savedXMLtechs"); //turns the variable "xmlpath" into a directory location with given name

            new GameObject().AddComponent<ExecuteButton>(); // Create instance after patches are applied to your mom
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(PopulationTable), "UpdatePopulation")] // use brackets to add an attribute to whatever class/domain comes below it (not make, add to objects below)
            static class LoadXMLTechsTOPopulation // Because of attribute, Harmony will use this every time UpdatePopulation is called by the game
            {
                static void Postfix(PopulationTable __instance) // Called after 
                {
                    foreach (var thing in __instance.m_FolderTechs) //a system operator that does a thing for each of the specified things in the parenthesis
                        Debug.Log(thing.m_folderName); // what the foreach does 

                    if (Directory.Exists(XMLPath)) //tests to see if the XMLpath directory exists
                    {
                        //does magical loading stuff below
                        var directory = new DirectoryInfo(XMLPath); // Create a new DirectoryInfo based off of XML path that can be used for later purposes
                        var files = directory.GetFiles(); // Get all the files from directory and return an array
                                                          //A simpler way is to one-line it, such as `foreach(var file in new DirectoryInfo(XMLPath).GetAllFiles())`

                        foreach (FileInfo file in files) // Iterate through the array; Use every object in the array one after the other until all are used
                        {
                            var tech = ExundXMLHandler.LoadXMLAsTech(file.FullName, Vector3.down * 1000f, Quaternion.Euler(0f, 0f, 0f)); //makes a new variable called "tech" derived from the TerraTech "Tank" class and gives it the value deserialized from the xml info,henmovest

                            var tankPreset = TankPreset.CreateInstance();
                            tankPreset.SaveTank(tech, false, false);
                            
                            __instance.m_FolderTechs[0].m_Presets.Add(tankPreset);

                            UnityEngine.GameObject.DestroyImmediate(tech.gameObject);
                        }
                    }
                    else
                    {
                        Debug.Log("XML folder does not exist! " + XMLPath);
                    }
                }
            }
        }
    }

    #region XMLHandler

    public static class ExundXMLHandler //static meaning it can only have static members, meaning that no part of this class needs to be instanced to use. Which it shouldn't be.
    {
        bool saveFail; // for use in my thing that I was gonna make BEFORE whitepaw did... something
        /// <summary>
        /// Save Techs as XML Files
        /// </summary>
        /// <param name="tech">Tech to save</param>
        /// <param name="path">Path of saving folder</param>
        public static void SaveTechAsXML(Tank tech, string path) //Any loading alternative? That's needed
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Console.WriteLine("XMLSave : Specified path \"" + path + "\" doesn't exists !");
                saveFail = false; //this will stay here as a reminder of that one time when I tried to make something on my own.   :(
                //rip in peace
            }
            XmlWriter saver = JsonConvert.Create(Path.Combine(path, tech.name + ".xml"), new XmlWriterSettings { Indent = true });

            saver.WriteStartDocument();
            saver.WriteStartElement("Tech");
            saver.WriteAttributeString("Name", tech.name);

            saver.WriteStartElement("Blocks");
            foreach (var block in tech.blockman.IterateBlocks())
            {
                saver.WriteStartElement("Block");
                saver.WriteAttributeString("Type", block.BlockType.ToString());
                if (tech.blockman.IsRootBlock(block)) saver.WriteAttributeString("IsRootBlock", "true");

                saver.WriteStartElement("BlockSpec");

                saver.WriteStartElement("OrthoRotation");
                saver.WriteString(block.cachedLocalRotation.rot.ToString());
                saver.WriteEndElement();

                var localPos = new IntVector3(block.cachedLocalPosition);
                saver.WriteStartElement("IntVector3");
                saver.WriteAttributeString("x", localPos.x.ToString());
                saver.WriteAttributeString("y", localPos.y.ToString());
                saver.WriteAttributeString("z", localPos.z.ToString());
                saver.WriteEndElement();

                saver.WriteEndElement();

                saver.WriteStartElement("Transform");

                var pos = block.trans.localPosition;
                saver.WriteStartElement("Position");
                saver.WriteAttributeString("x", pos.x.ToString());
                saver.WriteAttributeString("y", pos.y.ToString());
                saver.WriteAttributeString("z", pos.z.ToString());
                saver.WriteEndElement();

                var rotation = block.trans.localRotation.eulerAngles;
                saver.WriteStartElement("Rotation");
                saver.WriteAttributeString("x", rotation.x.ToString());
                saver.WriteAttributeString("y", rotation.y.ToString());
                saver.WriteAttributeString("z", rotation.z.ToString());
                saver.WriteEndElement();

                var scale = block.trans.localScale;
                saver.WriteStartElement("Scale");
                saver.WriteAttributeString("x", scale.x.ToString());
                saver.WriteAttributeString("y", scale.y.ToString());
                saver.WriteAttributeString("z", scale.z.ToString());
                saver.WriteEndElement();


                saver.WriteEndElement();
                saver.WriteEndElement();
            }
            saver.WriteEndElement();

            saver.WriteEndDocument();
            saver.Close();
        }

        public static Tank LoadXMLAsTech(string path, Vector3 position, Quaternion rotation)
        {
            XmlDocument TechXML = new XmlDocument();
            try
            {
                TechXML.Load(path);
            }
            catch
            {
                return null;
            }

            Tank tech = Singleton.Manager<ManSpawn>.inst.SpawnEmptyTech(0, position, rotation, true, false, TechXML.GetElementsByTagName("Tech")[0].Attributes["Name"].Value);

            for (int i = 0; i < TechXML.GetElementsByTagName("Block").Count; i++)
            {
                var BlockXML = TechXML.GetElementsByTagName("Block")[i];
                try
                {
                    if (BlockXML.Attributes["IsRootBlock"] != null)
                    {
                        BlockTypes blockType = (BlockTypes)Enum.Parse(typeof(BlockTypes), BlockXML.Attributes["Type"].Value);
                        TankBlock block = Singleton.Manager<ManSpawn>.inst.SpawnBlock(blockType, position - Vector3.one, rotation);

                        string OrthoRotationString = TechXML.SelectNodes("//BlockSpec/OrthoRotation")[i].InnerText;
                        OrthoRotation.r OrthoRot = (OrthoRotation.r)Enum.Parse(typeof(OrthoRotation.r), OrthoRotationString);

                        var cahedLocalPositionXML = TechXML.SelectNodes("//BlockSpec/IntVector3")[i].Attributes;
                        IntVector3 localPositionIntVector = new IntVector3(int.Parse(cahedLocalPositionXML["x"].Value), int.Parse(cahedLocalPositionXML["y"].Value), int.Parse(cahedLocalPositionXML["z"].Value));

                        tech.blockman.AddBlock(block, localPositionIntVector, new OrthoRotation(OrthoRot));

                        var localPositionXML = TechXML.SelectNodes("//Transform/Position")[i].Attributes;
                        Vector3 localPosition = new Vector3(float.Parse(localPositionXML["x"].Value), float.Parse(localPositionXML["y"].Value), float.Parse(localPositionXML["z"].Value));

                        var localRotationXML = TechXML.SelectNodes("//Transform/Rotation")[i].Attributes;
                        Vector3 localRotation = new Vector3(float.Parse(localRotationXML["x"].Value), float.Parse(localRotationXML["y"].Value), float.Parse(localRotationXML["z"].Value));

                        var localScaleXML = TechXML.SelectNodes("//Transform/Scale")[i].Attributes;
                        Vector3 localScale = new Vector3(float.Parse(localScaleXML["x"].Value), float.Parse(localScaleXML["y"].Value), float.Parse(localScaleXML["z"].Value));

                        block.trans.localPosition = localPosition;
                        block.trans.localRotation = Quaternion.Euler(localRotation);
                        block.trans.localScale = localScale;
                    }
                }
                catch { break; }

            }
            for (int i = 0; i < TechXML.GetElementsByTagName("Block").Count; i++)
            {
                try
                {

                    var BlockXML = TechXML.GetElementsByTagName("Block")[i];
                    if (BlockXML.Attributes["IsRootBlock"] != null) continue;

                    BlockTypes blockType = (BlockTypes)Enum.Parse(typeof(BlockTypes), BlockXML.Attributes["Type"].Value);
                    TankBlock block = Singleton.Manager<ManSpawn>.inst.SpawnBlock(blockType, position - Vector3.one, rotation);

                    string OrthoRotationString = TechXML.SelectNodes("//BlockSpec/OrthoRotation")[i].InnerText;
                    OrthoRotation.r OrthoRot = (OrthoRotation.r)Enum.Parse(typeof(OrthoRotation.r), OrthoRotationString);

                    var cahedLocalPositionXML = TechXML.SelectNodes("//BlockSpec/IntVector3")[i].Attributes;
                    IntVector3 localPositionIntVector = new IntVector3(int.Parse(cahedLocalPositionXML["x"].Value), int.Parse(cahedLocalPositionXML["y"].Value), int.Parse(cahedLocalPositionXML["z"].Value));

                    tech.blockman.AddBlock(block, localPositionIntVector, new OrthoRotation(OrthoRot));

                    //if (BlockXML.Attributes["IsRootBlock"] != null) tech.blockman.SetRootBlock(block);

                    var localPositionXML = TechXML.SelectNodes("//Transform/Position")[i].Attributes;
                    Vector3 localPosition = new Vector3(float.Parse(localPositionXML["x"].Value), float.Parse(localPositionXML["y"].Value), float.Parse(localPositionXML["z"].Value));

                    var localRotationXML = TechXML.SelectNodes("//Transform/Rotation")[i].Attributes;
                    Vector3 localRotation = new Vector3(float.Parse(localRotationXML["x"].Value), float.Parse(localRotationXML["y"].Value), float.Parse(localRotationXML["z"].Value));

                    var localScaleXML = TechXML.SelectNodes("//Transform/Scale")[i].Attributes;
                    Vector3 localScale = new Vector3(float.Parse(localScaleXML["x"].Value), float.Parse(localScaleXML["y"].Value), float.Parse(localScaleXML["z"].Value));

                    block.trans.localPosition = localPosition;
                    block.trans.localRotation = Quaternion.Euler(localRotation);
                    block.trans.localScale = localScale;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                }
            }

            if (tech.blockman.blockCount == 0) tech.blockman.AddBlock(Singleton.Manager<ManSpawn>.inst.SpawnBlock(BlockTypes.GSOCockpit_111, Vector3.zero, Quaternion.identity), IntVector3.zero);

            return tech;
        }
    }

    #endregion

    public class ExecuteButton : MonoBehaviour
    {
        //An instance, for example
        //When you make a new something
        //like a car
        //car is a type. Your car is an instance of type car.
        //where statics are more one-instanced. there cant be more than one. Like the atmosphere, or internet
        //Oh hello there

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
                ShowGUI = !ShowGUI; // invert ShowGUI value when J is pressed
        }

        bool ShowGUI = true;
        GUIStyle fontSize;

        public ExecuteButton() // Public constructor; This is called whenever a new instance is created. An alternative is 'void Start()' if it is in a MonoBehaviour class
        {
            fontSize = new GUIStyle(GUI.skin.button); // if you want to use it for a button
            fontSize.fontSize = 22; //How big you want the text, fill it out here 
            fontSize.alignment = TextAnchor.MiddleLeft;// This is an optional change with what you would like| oh yeah I used that in my thing
            fontSize.normal.textColor = Color.white;
        }

        void OnGUI()
        {
            if (!ShowGUI) // if it doesn't show a gui
                return; // Get the frick out of here if ShowGUI is not true
            
            GUI.TextField(new Rect(Screen.width * .7f, Screen.height * .8f, 500, Screen.height * 0.2f), log); // I do not know what you mean by parameters ( I know what pareameters are but not what they need to be)

            if (GUI.Button(new Rect(Screen.width * .7f, Screen.height * .8f - 30f /* Offset button to near bottom of the screen */, 500, 30), "Save current tech as .xml", fontSize))
            {
                Tank playerTank = Singleton.playerTank;
                ExundXMLHandler.SaveTechAsXML(playerTank, QPatch.XMLPath); // Save XML when button is pressed
            }
        }
    }
}
//just use existing one