using System;
using System.IO;
#if SOLARENGINE_KUAISHOU&&TUANJIE_2022_3_OR_NEWER
using KSWASM.editor;
#endif
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if SOLARENGINE_WECHAT && TUANJIE_WEIXINMINIGAME
using WeChatWASM;
#endif


    public  static class SolarEngineMiniGameConvertCore
    {
        private const string SolarEngineMiniGameConvertCoreLOG = "SolarEngineMiniGameConvertCore";
        const string dyCpJsPath = "Assets/StreamingAssets/__cp_js_files";
        const string sourceJsPath = "Assets/Plugins/SolarEngine/MiniGame/SolarEngineJsHelper.js";
      
#if (SOLARENGINE_BYTEDANCE|| SOLARENGINE_BYTEDANCE_CLOUD||SOLARENGINE_BYTEDANCE_STARK)&&TUANJIE_2022_3_OR_NEWER
       
        public class ByteDanceConvertCore
        { 
           

            [MenuItem("SolarEngineSDK/MiniGame/Tuanjie/BeforeBuildForTT")]
            public static void byteDanceConvertCore()
            {
                string _sourceJsPath = sourceJsPath;
                if (PackageChecker.IsUPMPackageInstalled())
                {
                    _sourceJsPath=Path.Combine(PackageChecker. GetPackagePath(), "Runtime/Plugins/MiniGame/SolarEngineJsHelper.js");

                }
                string cpJsPath = Path.Combine(dyCpJsPath, "SolarEngineJsHelper.js");
                try
                {
                    if(!Directory.Exists(dyCpJsPath))
                        Directory.CreateDirectory(dyCpJsPath);
                    if(File.Exists(cpJsPath))
                        File.Delete(cpJsPath);
                    File.Copy(_sourceJsPath, cpJsPath);
                    Debug.Log("SolarEngineJsHelper.js copied successfully.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to copy file: {e.Message}");
                }
            }
        }
#endif
#if SOLARENGINE_KUAISHOU&&TUANJIE_2022_3_OR_NEWER

        public class KSChatConvertCore : LifeCycleBase
        {

           [MenuItem("SolarEngineSDK/MiniGame/Tuanjie/ManuallAdd/BeforeBuildForKS")]
            public static void _beforeCopyDefault()
            {
                string _sourceJsPath = sourceJsPath;
                if (PackageChecker.IsUPMPackageInstalled())
                {
                    _sourceJsPath=Path.Combine(PackageChecker. GetPackagePath(), "Runtime/Plugins/MiniGame/SolarEngineJsHelper.js");

                }
                // 读取你的自定义模板目录并对其中的资源做动态修改
                var tmp = BuildTemplateHelper.CustomTemplateDir;
                if (!Directory.Exists(tmp))
                {
                    Debug.Log(
                        $"{SolarEngineMiniGameConvertCoreLOG} KS Custom template directory does not exist! Create ");

                    Directory.CreateDirectory(tmp);

                }
                string tmpPath = Path.Combine(tmp, "SolarEngineJsHelper.js");
                if( File.Exists(tmpPath))
                    File.Delete(tmpPath);
                
                File.Copy(_sourceJsPath, tmp);
                Debug.Log($"{SolarEngineMiniGameConvertCoreLOG} SolarEngineHelper.js Copy successfully");
            }
            
            public override void beforeCopyDefault()
            {
                string _sourceJsPath = sourceJsPath;
                if (PackageChecker.IsUPMPackageInstalled())
                {
                    _sourceJsPath=    Path.Combine(PackageChecker. GetPackagePath(), "Runtime/Plugins/MiniGame/SolarEngineJsHelper.js");

                }
                // 读取你的自定义模板目录并对其中的资源做动态修改
                var tmp = BuildTemplateHelper.CustomTemplateDir;
                if (!Directory.Exists(tmp))
                {
                    Debug.Log(
                        $"{SolarEngineMiniGameConvertCoreLOG} KS Custom template directory does not exist! Create ");

                    Directory.CreateDirectory(tmp);

                }
                string tmpPath = Path.Combine(tmp, "SolarEngineJsHelper.js");
                if( File.Exists(tmpPath))
                    File.Delete(tmpPath);
                
                File.Copy(_sourceJsPath, tmp);
                Debug.Log($"{SolarEngineMiniGameConvertCoreLOG} SolarEngineHelper.js Copy successfully");
                
            }

            // [MenuItem("SolarEngineSDK/MiniGame/Tuanjie/beforeCopyDefault")]

            public override void afterBuildTemplate()
            {
                
                Debug.Log($"{SolarEngineMiniGameConvertCoreLOG} WeChat afterBuildTemplate");
                addJsCode();
            }
            
            [MenuItem("SolarEngineSDK/MiniGame/Tuanjie/ManuallAdd/AfterBuildForKS")]
            static void addJsCode()
            {
                
                string jsCode = "require('./SolarEngineJsHelper.js');";
                KSEditorScriptObject config = UnityUtil.GetEditorConf("kuaishou", "Assets/KS-WASM-SDK-V2/Editor/MiniGameConfig.asset");
                string gamejsPath = Path.Combine(config.ProjectConf.DST, "minigame", "game.js");
                Debug.Log($"{SolarEngineMiniGameConvertCoreLOG} game.js path: {gamejsPath}");
                 
                
                File.AppendAllText(gamejsPath, jsCode);
                
                
                Debug.Log($"{SolarEngineMiniGameConvertCoreLOG} SolarEngineHelper Content inserted successfully");
                


            }

        

        }
#endif
           
#if SOLARENGINE_WECHAT && TUANJIE_WEIXINMINIGAME


        public class WxChatConvertCore: LifeCycleBase
        {
          
            [MenuItem("SolarEngineSDK/MiniGame/Tuanjie/ManuallAdd/BeforeBuildForWxChat")]
            public static void _beforeCopyDefault()
            {
                string _sourceJsPath = sourceJsPath;
                if (PackageChecker.IsUPMPackageInstalled())
                {
                    _sourceJsPath=    Path.Combine(PackageChecker. GetPackagePath(), "Runtime/Plugins/MiniGame/SolarEngineJsHelper.js");

                }
                // 读取你的自定义模板目录并对其中的资源做动态修改
                var tmp = BuildTemplateHelper.CustomTemplateDir;
                if (!Directory.Exists(tmp))
                {
                    Debug.Log(
                        $"{SolarEngineMiniGameConvertCoreLOG} WeChat Custom template directory does not exist! Create ");

                    Directory.CreateDirectory(tmp);

                }
                string tmpPath = Path.Combine(tmp, "SolarEngineJsHelper.js");
                if( File.Exists(tmpPath))
                    File.Delete(tmpPath);
                
                File.Copy(_sourceJsPath, tmp);
                Debug.Log($"{SolarEngineMiniGameConvertCoreLOG} SolarEngineHelper.js Copy successfully");
            }
            
            public override void beforeCopyDefault()
            {
                
                string _sourceJsPath = sourceJsPath;
                if (PackageChecker.IsUPMPackageInstalled())
                {
                    _sourceJsPath=    Path.Combine(PackageChecker. GetPackagePath(), "Runtime/Plugins/MiniGame/SolarEngineJsHelper.js");

                }
                // 读取你的自定义模板目录并对其中的资源做动态修改
                var tmp = BuildTemplateHelper.CustomTemplateDir;
                if (!Directory.Exists(tmp))
                {
                    Debug.Log(
                        $"{SolarEngineMiniGameConvertCoreLOG} WeChat Custom template directory does not exist! Create ");

                    Directory.CreateDirectory(tmp);

                }
                string tmpPath = Path.Combine(tmp, "SolarEngineJsHelper.js");
                if( File.Exists(tmpPath))
                    File.Delete(tmpPath);
                
                File.Copy(_sourceJsPath, tmp);
                Debug.Log($"{SolarEngineMiniGameConvertCoreLOG} SolarEngineHelper.js Copy successfully");
                
            }

            

            public override void afterBuildTemplate()
            {
                
                Debug.Log($"{SolarEngineMiniGameConvertCoreLOG} WeChat afterBuildTemplate");
                addJsCode();
            }
            
            [MenuItem("SolarEngineSDK/MiniGame/Tuanjie/ManuallAdd/AfterBuildForWxChat")]
            static void addJsCode()
            {
                string jsCode = "import 'SolarEngineJsHelper.js';";
                string gamejsPath = Path.Combine(UnityUtil.GetEditorConf().ProjectConf.DST, "minigame", "game.js");


                File.AppendAllText(gamejsPath, jsCode);


                Debug.Log($"{SolarEngineMiniGameConvertCoreLOG} SolarEngineHelper Content inserted successfully");



            }

        }
        
#endif
    
    }




