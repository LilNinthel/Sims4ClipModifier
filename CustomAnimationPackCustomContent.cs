using S4Studio;
using S4Studio.Blender;
using S4Studio.Data;
using S4Studio.Data.IO.Animation;
using S4Studio.Data.IO.Package;
using S4Studio.Data.Util;
using S4Studio.ViewModels;
using S4Studio.ViewModels.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sims4ClipModifier
{
    public class CustomAnimationPackCustomContent : AnimationPackCustomContent
    {
        public CustomAnimationPackCustomContent(IWindow window, IResourceProvider global, IDBPFPackage package) : base(window, global, package)
        {
        }

        public CustomAnimationPackCustomContent(IWindow window, IResourceProvider global, IDBPFPackage package, IAnimationListProvider animation_source) : base(window, global, package, animation_source)
        {
        }

        public void AddAnimation2()
        {
            MethodInfo dynMethod = this.GetType().GetMethod("ExecuteAddAnimation", BindingFlags.NonPublic | BindingFlags.Instance);

            dynMethod.Invoke(this, null);
        }

        private void ExecuteAddAnimation()
        {
            if (this.IsPosePack)
            {
                ClipResource blank = AnimationPackCustomContent.GenerateBlank(this.GlobalSource, this.BlankAgePrefix);
                blank.ClipName = this.PackInstanceName;
                blank.Clip.Name = this.NewAnimationName;
                blank.Clip.SourceAssetName = "";
                blank.Key.Instance = FNV.Hash64(this.NewAnimationName);
                this.Animations.Add(new AnimationContent(blank, this));
                this.SelectedAnimation = this.Animations.LastOrDefault<AnimationContent>();
                this.OnPropertyChanged("HasClips");
                this.OnPropertyChanged("IsPosePack");
            }
            //else
            //{
            //    this.NewAnimationName = "";
            //    this.NewAnimation = this.Window.ShowNewAnimationDialog(this);
            //}
        }

        public new void UpdateNames()
        {

        }

        public void RemoveAnimation()
        {
            this.Animations.Remove(this.SelectedAnimation);
            this.SelectedAnimation = this.Animations.LastOrDefault<AnimationContent>();
            foreach (ViewModelBase animation in (Collection<AnimationContent>)this.Animations)
                animation.OnPropertyChanged("Index");
            this.SavePosePack();
            this.GenerateHeaders();
        }

        public void ImportAnimation(string clipPath)
        {
            AnimationPackCustomContent packCustomContent = this;
            bool found;
            // ISSUE: explicit non-virtual call
            BlenderUtilities blender_util = new BlenderUtilities(Settings.Default.BlenderPath);
            // ISSUE: explicit non-virtual call
            // ISSUE: explicit non-virtual call
            string str1 = Path.Combine(blender_util.ScriptWorkingPath, Guid.NewGuid().ToString());
            try
            {
                ClipResource clip = this.SelectedAnimation.Clip;
                if (!Directory.Exists(str1))
                    Directory.CreateDirectory(str1);
                string str2 = Path.Combine(str1, "CLIP") + "\\";
                string clip_path = Path.Combine(str2, "s4s.animation");
                Directory.CreateDirectory(str2);
                using (FileStream fileStream = File.Create(clip_path))
                    clip.Write((Stream)fileStream);
                string prefix = this.SelectedRigOption.Prefix;
                string str3 = Path.Combine(str1, Path.GetFileName(clipPath));
                File.Copy(clipPath, str3, true);
                blender_util.InstallAddon();
                blender_util.ImportClip(str3, clip_path, prefix);
                //this.Window.Invoke((Action)(() =>
                //{
                    using (FileStream fileStream = File.OpenRead(clip_path))
                        clip.Read((Stream)fileStream);
                    clip.Clip.SourceAssetName = Path.GetFileName(clipPath);
                    this.GenerateHeaders();
                    this.SelectedAnimation.OnPropertyChanged("FileName");
                //}));
            }
            //catch (Sims4StudioBlenderException ex)
            //{
            //    this.Window.Invoke((Action)(() => this.Window.Alert(ex.Message, ex.Title)));
            //}
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                Directory.Delete(str1, true);
            }
        }
    }
}
