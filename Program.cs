using S4S;
using S4Studio;
using S4Studio.Blender;
using S4Studio.Converters;
using S4Studio.Core;
using S4Studio.Data;
using S4Studio.Data.FileDb;
using S4Studio.Data.FileDb.Models;
using S4Studio.Data.IO.Animation;
using S4Studio.Data.IO.CAS;
using S4Studio.Data.IO.CAS.Geometry;
using S4Studio.Data.IO.Core;
using S4Studio.Data.IO.Images;
using S4Studio.Data.IO.Package;
using S4Studio.Data.IO.Tags;
using S4Studio.Data.IO.Tuning;
using S4Studio.Data.Util;
using S4Studio.Rendering;
using S4Studio.Shared;
using S4Studio.Util;
using S4Studio.ViewModels;
using S4Studio.ViewModels.Animation;
using S4Studio.ViewModels.CAS;
using Sims4ClipModifier;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Media.Converters;
using System.Windows.Shapes;
using Path = System.IO.Path;
using CommandLine;
using System.Xml.Linq;

namespace Sims4Hasher
{
    class Program
    {
        public class Options
        {
            [Option('m',"mode")]
            public string Mode { get; set; }

            [Option("bodyType",HelpText ="Valid values: a, b, accessory, bottom")]
            public string BodyType { get; set; }

            [Option("gender", HelpText ="Valid values: f, m, female, male")]
            public string Gender { get; set; }

            [Option("specularFile")]
            public string SpecularFile { get; set; }

            [Option("specularId")]
            public string SpecularId { get; set; }

            [Option("specularUseExisting")]
            public bool SpecularUseExisting { get; set; }

            [Option("normalMapFile")]
            public string NormalMapFile { get; set; }

            [Option("normalMapId")]
            public string NormalMapId { get; set; }

            [Option("normalMapUseExisting")]
            public bool NormalMapUseExisting { get; set; }

            [Option("modelFile",HelpText="The full path to the .blend file to be used for LOD 0")]
            public string ModelFile { get; set; }

            [Option("modelId",HelpText ="The instance ID of an existing RegionMap for the LOD 0 model. This RegionMap must be discoverable by Sims 4 Studio.")]
            public string ModelId { get; set; }

            [Option("modelUseExisting", HelpText ="Use the RegionMap that is already in the package for any new swatches. Cannot be used for new packages")]
            public bool ModelUseExisting { get; set; }

            [Option("packageName",HelpText ="The name of the package WITHOUT the extension or directory. The package will be saved in the directory set by the --diaperTextureRoot parameter Example: --packageName LilNinthel_example_package")]
            public string PackageName { get; set; }

            [Option("diaperTextureRoot",  HelpText ="The full path to the root directory for this variant. This should only contain textures for a single gender and diaper height. There should be folders for each diaper variety/brand within, each containing PNG files named for the state such as w0m1.png")]
            public string DiaperTextureRoot { get; set; }

            [Option("visibleStates",Separator =',',HelpText ="Comma separated list of wet/messy states to show in CAS UI. Dry and clean state is always visible. Example: --visibleStates w1,w3m2,m2")]
            public IEnumerable<string> VisibleStates { get; set; }

            [Option("includeOutfitCategories",HelpText ="Using this flag will tag all swatches with every standard CAS outfit categories except Swimwear. Omitting it will not mark the swatches with any categories and will not be visible in CAS when outfit filters are used")]
            public bool IncludeOutfitCategories { get; set; }
        }

        private static Random random = new Random();

        private static List<string> infantPrefixes = new List<string>{ "i_", "a2i_", "i2i_", "i2a_", "i2o_" };

        private static List<string> toddlerPrefixes = new List<string> { "p_", "a2p_", "p2p_", "p2a_", "p2o_" };

        private static List<string> childPrefixes = new List<string> { "c_", "a2c_", "c2c_", "c2a_", "c2o_" };

        private static HashSet<string> ikTargetReplace = new HashSet<string> { "_IKtarget_toddler_butt_", "_IKtarget_toddler_L_foot_", "_IKtarget_toddler_R_foot_", "_IKtarget_infant_butt_", "_IKtarget_infant_L_foot_", "_IKtarget_infant_R_foot_" };


        class BoneHashes
        {
            public static uint b__backneck_slot = FNV.Hash32("b__backneck_slot");
            public static uint b__carry__ = FNV.Hash32("b__carry__");
            public static uint b__cas_chin__ = FNV.Hash32("b__cas_chin__");
            public static uint b__cas_glasses__ = FNV.Hash32("b__cas_glasses__");
            public static uint b__cas_jawcomp__ = FNV.Hash32("b__cas_jawcomp__");
            public static uint b__cas_l_breast__ = FNV.Hash32("b__cas_l_breast__");
            public static uint b__cas_l_eyearea__ = FNV.Hash32("b__cas_l_eyearea__");
            public static uint b__cas_l_eyescale__ = FNV.Hash32("b__cas_l_eyescale__");
            public static uint b__cas_l_nostril__ = FNV.Hash32("b__cas_l_nostril__");
            public static uint b__cas_lowermoutharea__ = FNV.Hash32("b__cas_lowermoutharea__");
            public static uint b__cas_nosearea__ = FNV.Hash32("b__cas_nosearea__");
            public static uint b__cas_nosebridge__ = FNV.Hash32("b__cas_nosebridge__");
            public static uint b__cas_nosetip__ = FNV.Hash32("b__cas_nosetip__");
            public static uint b__cas_r_breast__ = FNV.Hash32("b__cas_r_breast__");
            public static uint b__cas_r_eyearea__ = FNV.Hash32("b__cas_r_eyearea__");
            public static uint b__cas_r_eyescale__ = FNV.Hash32("b__cas_r_eyescale__");
            public static uint b__cas_r_nostril__ = FNV.Hash32("b__cas_r_nostril__");
            public static uint b__cas_uppermoutharea__ = FNV.Hash32("b__cas_uppermoutharea__");
            public static uint b__head__ = FNV.Hash32("b__head__");
            public static uint b__jaw__ = FNV.Hash32("b__jaw__");
            public static uint b__l_armexportpole__ = FNV.Hash32("b__l_armexportpole__");
            public static uint b__l_backbellytarget_slot = FNV.Hash32("b__l_backbellytarget_slot");
            public static uint b__l_backtarget_slot = FNV.Hash32("b__l_backtarget_slot");
            public static uint b__l_bellytarget_slot = FNV.Hash32("b__l_bellytarget_slot");
            public static uint b__l_bracelet_slot = FNV.Hash32("b__l_bracelet_slot");
            public static uint b__l_breasttarget_slot = FNV.Hash32("b__l_breasttarget_slot");
            public static uint b__l_calf__ = FNV.Hash32("b__l_calf__");
            public static uint b__l_carry_slot = FNV.Hash32("b__l_carry_slot");
            public static uint b__l_cheek__ = FNV.Hash32("b__l_cheek__");
            public static uint b__l_chesttarget_slot = FNV.Hash32("b__l_chesttarget_slot");
            public static uint b__l_clavicle__ = FNV.Hash32("b__l_clavicle__");
            public static uint b__l_earring_slot = FNV.Hash32("b__l_earring_slot");
            public static uint b__l_elbow__ = FNV.Hash32("b__l_elbow__");
            public static uint b__l_eye__ = FNV.Hash32("b__l_eye__");
            public static uint b__l_foot__ = FNV.Hash32("b__l_foot__");
            public static uint b__l_forearm__ = FNV.Hash32("b__l_forearm__");
            public static uint b__l_forearmtarget_slot = FNV.Hash32("b__l_forearmtarget_slot");
            public static uint b__l_forearmtwist__ = FNV.Hash32("b__l_forearmtwist__");
            public static uint b__l_frontbellytarget_slot = FNV.Hash32("b__l_frontbellytarget_slot");
            public static uint b__l_frontcalftarget_slot = FNV.Hash32("b__l_frontcalftarget_slot");
            public static uint b__l_fronttorsotarget_slot = FNV.Hash32("b__l_fronttorsotarget_slot");
            public static uint b__l_hand__ = FNV.Hash32("b__l_hand__");
            public static uint b__l_handdangle_slot = FNV.Hash32("b__l_handdangle_slot");
            public static uint b__l_inbrow__ = FNV.Hash32("b__l_inbrow__");
            public static uint b__l_incalftarget_slot = FNV.Hash32("b__l_incalftarget_slot");
            public static uint b__l_index0__ = FNV.Hash32("b__l_index0__");
            public static uint b__l_index1__ = FNV.Hash32("b__l_index1__");
            public static uint b__l_index2__ = FNV.Hash32("b__l_index2__");
            public static uint b__l_kneetarget_slot = FNV.Hash32("b__l_kneetarget_slot");
            public static uint b__l_legexportpole__ = FNV.Hash32("b__l_legexportpole__");
            public static uint b__l_lolid__ = FNV.Hash32("b__l_lolid__");
            public static uint b__l_lolip__ = FNV.Hash32("b__l_lolip__");
            public static uint b__l_lowbacktarget_slot = FNV.Hash32("b__l_lowbacktarget_slot");
            public static uint b__l_mid0__ = FNV.Hash32("b__l_mid0__");
            public static uint b__l_mid1__ = FNV.Hash32("b__l_mid1__");
            public static uint b__l_mid2__ = FNV.Hash32("b__l_mid2__");
            public static uint b__l_midbrow__ = FNV.Hash32("b__l_midbrow__");
            public static uint b__l_mouth__ = FNV.Hash32("b__l_mouth__");
            public static uint b__l_outbrow__ = FNV.Hash32("b__l_outbrow__");
            public static uint b__l_outshortsleeve_slot = FNV.Hash32("b__l_outshortsleeve_slot");
            public static uint b__l_outthightarget_slot = FNV.Hash32("b__l_outthightarget_slot");
            public static uint b__l_pinky0__ = FNV.Hash32("b__l_pinky0__");
            public static uint b__l_pinky1__ = FNV.Hash32("b__l_pinky1__");
            public static uint b__l_pinky2__ = FNV.Hash32("b__l_pinky2__");
            public static uint b__l_prop__ = FNV.Hash32("b__l_prop__");
            public static uint b__l_ring_slot = FNV.Hash32("b__l_ring_slot");
            public static uint b__l_ring0__ = FNV.Hash32("b__l_ring0__");
            public static uint b__l_ring1__ = FNV.Hash32("b__l_ring1__");
            public static uint b__l_ring2__ = FNV.Hash32("b__l_ring2__");
            public static uint b__l_shoulderbladetarget_slot = FNV.Hash32("b__l_shoulderbladetarget_slot");
            public static uint b__l_shouldertarget_slot = FNV.Hash32("b__l_shouldertarget_slot");
            public static uint b__l_shouldertwist__ = FNV.Hash32("b__l_shouldertwist__");
            public static uint b__l_sidebacktorsotarget_slot = FNV.Hash32("b__l_sidebacktorsotarget_slot");
            public static uint b__l_skirt__ = FNV.Hash32("b__l_skirt__");
            public static uint b__l_squint__ = FNV.Hash32("b__l_squint__");
            public static uint b__l_stigmata = FNV.Hash32("b__l_stigmata");
            public static uint b__l_thigh__ = FNV.Hash32("b__l_thigh__");
            public static uint b__l_thighfronttarget_slot = FNV.Hash32("b__l_thighfronttarget_slot");
            public static uint b__l_thightarget_slot = FNV.Hash32("b__l_thightarget_slot");
            public static uint b__l_thightwist__ = FNV.Hash32("b__l_thightwist__");
            public static uint b__l_thumb0__ = FNV.Hash32("b__l_thumb0__");
            public static uint b__l_thumb1__ = FNV.Hash32("b__l_thumb1__");
            public static uint b__l_thumb2__ = FNV.Hash32("b__l_thumb2__");
            public static uint b__l_toe__ = FNV.Hash32("b__l_toe__");
            public static uint b__l_uplid__ = FNV.Hash32("b__l_uplid__");
            public static uint b__l_uplip__ = FNV.Hash32("b__l_uplip__");
            public static uint b__l_upperarm__ = FNV.Hash32("b__l_upperarm__");
            public static uint b__lolip__ = FNV.Hash32("b__lolip__");
            public static uint b__mouth_slot = FNV.Hash32("b__mouth_slot");
            public static uint b__neck__ = FNV.Hash32("b__neck__");
            public static uint b__neck_slot = FNV.Hash32("b__neck_slot");
            public static uint b__pelvis__ = FNV.Hash32("b__pelvis__");
            public static uint b__r_armexportpole__ = FNV.Hash32("b__r_armexportpole__");
            public static uint b__r_backbellytarget_slot = FNV.Hash32("b__r_backbellytarget_slot");
            public static uint b__r_backtarget_slot = FNV.Hash32("b__r_backtarget_slot");
            public static uint b__r_bellytarget_slot = FNV.Hash32("b__r_bellytarget_slot");
            public static uint b__r_bracelet_slot = FNV.Hash32("b__r_bracelet_slot");
            public static uint b__r_breasttarget_slot = FNV.Hash32("b__r_breasttarget_slot");
            public static uint b__r_calf__ = FNV.Hash32("b__r_calf__");
            public static uint b__r_carry_slot = FNV.Hash32("b__r_carry_slot");
            public static uint b__r_cheek__ = FNV.Hash32("b__r_cheek__");
            public static uint b__r_chesttarget_slot = FNV.Hash32("b__r_chesttarget_slot");
            public static uint b__r_clavicle__ = FNV.Hash32("b__r_clavicle__");
            public static uint b__r_earring_slot = FNV.Hash32("b__r_earring_slot");
            public static uint b__r_elbow__ = FNV.Hash32("b__r_elbow__");
            public static uint b__r_eye__ = FNV.Hash32("b__r_eye__");
            public static uint b__r_foot__ = FNV.Hash32("b__r_foot__");
            public static uint b__r_forearm__ = FNV.Hash32("b__r_forearm__");
            public static uint b__r_forearmtarget_slot = FNV.Hash32("b__r_forearmtarget_slot");
            public static uint b__r_forearmtwist__ = FNV.Hash32("b__r_forearmtwist__");
            public static uint b__r_frontbellytarget_slot = FNV.Hash32("b__r_frontbellytarget_slot");
            public static uint b__r_frontcalftarget_slot = FNV.Hash32("b__r_frontcalftarget_slot");
            public static uint b__r_fronttorsotarget_slot = FNV.Hash32("b__r_fronttorsotarget_slot");
            public static uint b__r_hand__ = FNV.Hash32("b__r_hand__");
            public static uint b__r_handdangle_slot = FNV.Hash32("b__r_handdangle_slot");
            public static uint b__r_inbrow__ = FNV.Hash32("b__r_inbrow__");
            public static uint b__r_incalftarget_slot = FNV.Hash32("b__r_incalftarget_slot");
            public static uint b__r_index0__ = FNV.Hash32("b__r_index0__");
            public static uint b__r_index1__ = FNV.Hash32("b__r_index1__");
            public static uint b__r_index2__ = FNV.Hash32("b__r_index2__");
            public static uint b__r_kneetarget_slot = FNV.Hash32("b__r_kneetarget_slot");
            public static uint b__r_legexportpole__ = FNV.Hash32("b__r_legexportpole__");
            public static uint b__r_lolid__ = FNV.Hash32("b__r_lolid__");
            public static uint b__r_lolip__ = FNV.Hash32("b__r_lolip__");
            public static uint b__r_lowbacktarget_slot = FNV.Hash32("b__r_lowbacktarget_slot");
            public static uint b__r_mid0__ = FNV.Hash32("b__r_mid0__");
            public static uint b__r_mid1__ = FNV.Hash32("b__r_mid1__");
            public static uint b__r_mid2__ = FNV.Hash32("b__r_mid2__");
            public static uint b__r_midbrow__ = FNV.Hash32("b__r_midbrow__");
            public static uint b__r_mouth__ = FNV.Hash32("b__r_mouth__");
            public static uint b__r_outbrow__ = FNV.Hash32("b__r_outbrow__");
            public static uint b__r_outshortsleeve_slot = FNV.Hash32("b__r_outshortsleeve_slot");
            public static uint b__r_outthightarget_slot = FNV.Hash32("b__r_outthightarget_slot");
            public static uint b__r_pinky0__ = FNV.Hash32("b__r_pinky0__");
            public static uint b__r_pinky1__ = FNV.Hash32("b__r_pinky1__");
            public static uint b__r_pinky2__ = FNV.Hash32("b__r_pinky2__");
            public static uint b__r_prop__ = FNV.Hash32("b__r_prop__");
            public static uint b__r_ring_slot = FNV.Hash32("b__r_ring_slot");
            public static uint b__r_ring0__ = FNV.Hash32("b__r_ring0__");
            public static uint b__r_ring1__ = FNV.Hash32("b__r_ring1__");
            public static uint b__r_ring2__ = FNV.Hash32("b__r_ring2__");
            public static uint b__r_shoulderbladetarget_slot = FNV.Hash32("b__r_shoulderbladetarget_slot");
            public static uint b__r_shouldertarget_slot = FNV.Hash32("b__r_shouldertarget_slot");
            public static uint b__r_shouldertwist__ = FNV.Hash32("b__r_shouldertwist__");
            public static uint b__r_sidebacktorsotarget_slot = FNV.Hash32("b__r_sidebacktorsotarget_slot");
            public static uint b__r_skirt__ = FNV.Hash32("b__r_skirt__");
            public static uint b__r_squint__ = FNV.Hash32("b__r_squint__");
            public static uint b__r_stigmata = FNV.Hash32("b__r_stigmata");
            public static uint b__r_thigh__ = FNV.Hash32("b__r_thigh__");
            public static uint b__r_thighfronttarget_slot = FNV.Hash32("b__r_thighfronttarget_slot");
            public static uint b__r_thightarget_slot = FNV.Hash32("b__r_thightarget_slot");
            public static uint b__r_thightwist__ = FNV.Hash32("b__r_thightwist__");
            public static uint b__r_thumb0__ = FNV.Hash32("b__r_thumb0__");
            public static uint b__r_thumb1__ = FNV.Hash32("b__r_thumb1__");
            public static uint b__r_thumb2__ = FNV.Hash32("b__r_thumb2__");
            public static uint b__r_toe__ = FNV.Hash32("b__r_toe__");
            public static uint b__r_uplid__ = FNV.Hash32("b__r_uplid__");
            public static uint b__r_uplip__ = FNV.Hash32("b__r_uplip__");
            public static uint b__r_upperarm__ = FNV.Hash32("b__r_upperarm__");
            public static uint b__root__ = FNV.Hash32("b__root__");
            public static uint b__root__bind = FNV.Hash32("b__root__bind");
            public static uint b__root_bind__ = FNV.Hash32("b__root_bind__");
            public static uint b__spine0__ = FNV.Hash32("b__spine0__");
            public static uint b__spine1__ = FNV.Hash32("b__spine1__");
            public static uint b__spine2__ = FNV.Hash32("b__spine2__");
            public static uint b__subroot__0 = FNV.Hash32("b__subroot__0");
            public static uint b__subroot__1 = FNV.Hash32("b__subroot__1");
            public static uint b__subroot__10 = FNV.Hash32("b__subroot__10");
            public static uint b__subroot__11 = FNV.Hash32("b__subroot__11");
            public static uint b__subroot__2 = FNV.Hash32("b__subroot__2");
            public static uint b__subroot__3 = FNV.Hash32("b__subroot__3");
            public static uint b__subroot__4 = FNV.Hash32("b__subroot__4");
            public static uint b__subroot__5 = FNV.Hash32("b__subroot__5");
            public static uint b__subroot__6 = FNV.Hash32("b__subroot__6");
            public static uint b__subroot__7 = FNV.Hash32("b__subroot__7");
            public static uint b__subroot__8 = FNV.Hash32("b__subroot__8");
            public static uint b__subroot__9 = FNV.Hash32("b__subroot__9");
            public static uint b__uplip__ = FNV.Hash32("b__uplip__");

            public static HashSet<uint> root =
                new HashSet<uint>
                {
                    b__root__,
                    b__root__bind,
                    b__root_bind__
                };

            public static HashSet<uint> leftLeg =
                new HashSet<uint>
                {
                    b__l_calf__,
                    b__l_incalftarget_slot,
                    b__l_kneetarget_slot,
                    b__l_thigh__,
                    b__l_thightwist__,
                    b__l_thightarget_slot,
                    b__l_thighfronttarget_slot
                };

            public static HashSet<uint> leftFoot =
                new HashSet<uint>
                {
                    b__l_foot__,
                    b__l_toe__
                };

            public static HashSet<uint> rightLeg =
                new HashSet<uint>
                {
                    b__r_calf__,
                    b__r_incalftarget_slot,
                    b__r_kneetarget_slot,
                    b__r_thigh__,
                    b__r_thightwist__,
                    b__r_thightarget_slot,
                    b__r_thighfronttarget_slot
                };

            public static HashSet<uint> rightFoot =
                new HashSet<uint>
                {
                    b__r_foot__,
                    b__r_toe__
                };

            public static HashSet<uint> bothFeet = new HashSet<uint>(leftFoot.Concat(rightFoot));

            public static HashSet<uint> bothLegs = new HashSet<uint>(leftLeg.Concat(rightLeg));

            public static HashSet<uint> bothLegsAndFeet = new HashSet<uint>(bothFeet.Concat(bothLegs));

            public static HashSet<uint> leftArm =
                new HashSet<uint>
                {
                    b__l_elbow__,
                    b__l_forearm__,
                    b__l_forearmtarget_slot,
                    b__l_forearmtwist__,
                    b__l_upperarm__
                };

            public static HashSet<uint> leftHand =
                new HashSet<uint>
                {
                    b__l_hand__,
                    b__l_handdangle_slot,
                    b__l_index0__,
                    b__l_index1__,
                    b__l_index2__,
                    b__l_mid0__,
                    b__l_mid1__,
                    b__l_mid2__,
                    b__l_pinky0__,
                    b__l_pinky1__,
                    b__l_pinky2__,
                    b__l_ring_slot,
                    b__l_ring0__,
                    b__l_ring1__,
                    b__l_ring2__,
                    b__l_stigmata,
                    b__l_thumb0__,
                    b__l_thumb1__,
                    b__l_thumb2__
                };

            public static HashSet<uint> rightArm =
                new HashSet<uint>
                {
                    b__r_elbow__,
                    b__r_forearm__,
                    b__r_forearmtarget_slot,
                    b__r_forearmtwist__,
                    b__r_upperarm__
                };

            public static HashSet<uint> rightHand =
                new HashSet<uint>
                {
                    b__r_hand__,
                    b__r_handdangle_slot,
                    b__r_index0__,
                    b__r_index1__,
                    b__r_index2__,
                    b__r_mid0__,
                    b__r_mid1__,
                    b__r_mid2__,
                    b__r_pinky0__,
                    b__r_pinky1__,
                    b__r_pinky2__,
                    b__r_ring_slot,
                    b__r_ring0__,
                    b__r_ring1__,
                    b__r_ring2__,
                    b__r_stigmata,
                    b__r_thumb0__,
                    b__r_thumb1__,
                    b__r_thumb2__
                };

            public static HashSet<uint> bothHands = new HashSet<uint>(leftHand.Concat(rightHand));

            public static HashSet<uint> bothArms = new HashSet<uint>(leftArm.Concat(rightArm));

            public static HashSet<uint> bothArmsAndHands = new HashSet<uint>(bothHands.Concat(bothArms));
        }

        static void Main(string[] args)
        {

            Console.WriteLine("Program started");
            AppModel.Instance.Init();

            IResourceProvider GlobalSource = AppModel.Instance.GlobalFiles;

            FiletableAnimationListProvider alp = new FiletableAnimationListProvider();

            //var items = (IEnumerable<AnimationItem>)alp.AnimationItems.Select<ClipContentMetadata, AnimationItem>(x => new AnimationItem(x, GlobalSource)).OrderBy<AnimationItem, string>((Func<AnimationItem, string>)(x => x.ContentMetadata.Name));

            //var clipList = items.Select(i => i.Name).ToHashSet();

            //AppModel.Instance.DoInit();

            BlenderUtilities blender_util = new BlenderUtilities(Settings.Default.BlenderPath);
            //blender_util.CurrentVersion = "3.6";
            //blender_util.InstallAddon();

            //foreach (var ai in items)
            //{
            //    foreach (var prefix in childPrefixes)
            //    {
            //        if (ai.Name.StartsWith(prefix))
            //        {
            //            if(ai.Name.Contains("monkeyBar", StringComparison.InvariantCultureIgnoreCase) || ai.Name.Contains("jungleGym", StringComparison.InvariantCultureIgnoreCase))
            //            {
            //                var clip = GlobalSource.Find(ClipResource.Type, (ulong)ai.ContentMetadata.Instance).Data<ClipResource>();
            //                string name = clip.Clip.Name;
            //                clip.Clip.SourceAssetName = name;
            //                //Console.WriteLine(ai.Name);
            //                Console.WriteLine(clip.ClipName);

            //                ExportClip(GlobalSource, clip, SimBody.YF, @"M:\Sims 4 Child Clip Exports\Adult", clipList, blender_util);
            //                //return;
            //                //ExportClip(GlobalSource, clip, SimBody.CU, @"M:\Sims 4 Toddler Clip Exports\Child", clipList, blender_util);
            //                ExportClip(GlobalSource, clip, SimBody.CU, @"M:\Sims 4 Child Clip Exports\Original", clipList, blender_util);
            //                //return;
            //                //continue;
            //            }
            //        }
            //    }
            //    //foreach (var prefix in toddlerPrefixes)
            //    //{
            //    //    if (ai.Name.StartsWith(prefix))
            //    //    {
            //    //        var clip = GlobalSource.Find(ClipResource.Type, (ulong)ai.ContentMetadata.Instance).Data<ClipResource>();
            //    //        string name = clip.Clip.Name;
            //    //        clip.Clip.SourceAssetName = name;
            //    //        //Console.WriteLine(ai.Name);
            //    //        Console.WriteLine(clip.ClipName);

            //    //        ExportClip(GlobalSource, clip, SimBody.YF, @"M:\Sims 4 Toddler Clip Exports\Adult", clipList, blender_util);
            //    //        //return;
            //    //        ExportClip(GlobalSource, clip, SimBody.CU, @"M:\Sims 4 Toddler Clip Exports\Child", clipList, blender_util);
            //    //        ExportClip(GlobalSource, clip, SimBody.PU, @"M:\Sims 4 Toddler Clip Exports\Original", clipList, blender_util);
            //    //        //return;
            //    //        continue;
            //    //    }
            //    //}
            //    //foreach (var prefix in infantPrefixes)
            //    //{
            //    //    if (ai.Name.StartsWith(prefix))
            //    //    {
            //    //        var clip = GlobalSource.Find(ClipResource.Type, (ulong)ai.ContentMetadata.Instance).Data<ClipResource>();
            //    //        string name = clip.Clip.Name;
            //    //        clip.Clip.SourceAssetName = name;
            //    //        //Console.WriteLine(ai.Name);
            //    //        Console.WriteLine(clip.ClipName);

            //    //        ExportClip(GlobalSource, clip, SimBody.YF, @"M:\Sims 4 Infant Clip Exports\Adult", clipList, blender_util);
            //    //        //return;
            //    //        ExportClip(GlobalSource, clip, SimBody.CU, @"M:\Sims 4 Infant Clip Exports\Child", clipList, blender_util);
            //    //        ExportClip(GlobalSource, clip, SimBody.IU, @"M:\Sims 4 Infant Clip Exports\Original", clipList, blender_util);
            //    //        //return;
            //    //        continue;
            //    //    }
            //    //}
            //}
            
            

            var originalClipDir = @"M:\Sims 4 Clip Export";

            Dictionary<string, string> fileLookup = new();

            foreach (var file in Directory.GetFiles(originalClipDir))
            {
                var clipName = file.Split('.')[1].Split('.')[0];

                fileLookup.Add(clipName, file);
            }

            //Console.WriteLine(AppGlobals.StudioSettingsPath);
            if (args.Length == 0)
            {
                //Console.WriteLine(posePack);
                var package = new DBPFPackage(@"G:\Modding\PC\Sims 4 Mods\Projects\IwnBedwetting Extended Plus\Workspace\DummyPosePack.package");


                var infantRootDir = @"M:\Sims 4 Infant Clip Exports";
                var toddlerRootDir = @"M:\Sims 4 Toddler Clip Exports";
                var childRootDir = @"M:\Sims 4 Child Clip Exports";

                var clipSubfolderName = "Converted Clips";

                var adultSubfolderName = "Adult";
                var childSubfolderName = "Child";

                

                var infantAdultBlendDir = Path.Combine(infantRootDir, adultSubfolderName);
                var infantChildBlendDir = Path.Combine(infantRootDir, childSubfolderName);
                var toddlerAdultBlendDir = Path.Combine(toddlerRootDir, adultSubfolderName);
                var toddlerChildBlendDir = Path.Combine(toddlerRootDir, childSubfolderName);
                var childAdultBlendDir = Path.Combine(childRootDir, adultSubfolderName);

                var infantAdultClipsDir = Path.Combine(Path.Combine(infantRootDir, clipSubfolderName), adultSubfolderName);
                var infantChildClipsDir = Path.Combine(Path.Combine(infantRootDir, clipSubfolderName), childSubfolderName);
                var toddlerAdultClipsDir = Path.Combine(Path.Combine(toddlerRootDir, clipSubfolderName), adultSubfolderName);
                var toddlerChildClipsDir = Path.Combine(Path.Combine(toddlerRootDir, clipSubfolderName), childSubfolderName);
                var childAdultClipsDir = Path.Combine(Path.Combine(childRootDir, clipSubfolderName), adultSubfolderName);


                IList<Tuple<string, string, string, string>> dirAgeList = new List<Tuple<string, string, string, string>>
                {
                    //new Tuple<string, string, string, string>(infantAdultBlendDir, "i", infantAdultClipsDir, "a"),
                    //new Tuple<string, string, string, string>(infantChildBlendDir, "i", infantChildClipsDir, "c"),
                    //new Tuple<string, string, string, string>(toddlerAdultBlendDir, "p", toddlerAdultClipsDir, "a"),
                    //new Tuple<string, string, string, string>(toddlerChildBlendDir, "p", toddlerChildClipsDir, "c"),
                    new Tuple<string, string, string, string>(childAdultBlendDir, "c", childAdultClipsDir, "a")
                };



                foreach(var t in dirAgeList)
                {
                    foreach (var file in Directory.GetFiles(t.Item1, "*.blend"))
                    {
                        var origClipName = getOriginalClipName(Path.GetFileName(file).Split(".blend")[0], t.Item2, fileLookup);
                        if (origClipName == null)
                        {
                            Console.WriteLine($"Cannot find original clip for {file}");
                            continue;
                        }

                        if (!file.Contains("kidsTent", StringComparison.InvariantCultureIgnoreCase))
                        {
                            continue;
                        }

                            var originalClip = LoadClip(fileLookup[origClipName]);


                        package.Add(originalClip);
                        package.Add(new ClipHeaderResource(originalClip));
                        var posePack = new CustomAnimationPackCustomContent(null, GlobalSource, package, alp);
                        posePack.SelectedAnimation = posePack.Animations.Last();

                        posePack.ImportAnimation(file);

                        originalClip = LoadClip(fileLookup[origClipName]);

                        var clip = posePack.Animations.Last().Clip;

                        clip.Clip.F1Palette = originalClip.Clip.F1Palette;
                        clip.Duration = originalClip.Duration;
                        clip.Clip.TickCount = originalClip.Clip.TickCount;
                        clip.ClipName = Path.GetFileName(file).Split(".blend")[0];
                        clip.Clip.Name = Path.GetFileName(file).Split(".blend")[0];

                        foreach(var slot in clip.SlotAssignments)
                        {
                            if(ikTargetReplace.Contains(slot.TargetName))
                            {
                                switch(t.Item4)
                                {
                                    case "a":
                                        slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_child_").Replace("_child_", "_");
                                        break;
                                    case "c":
                                        slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_child_");
                                        break;
                                }
                            }
                        }

                        foreach (var fd in originalClip.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation))
                        {
                            if (!clip.Clip.Channels.Any(c => c.ChannelSubTarget == fd.ChannelSubTarget && c.FrameCount == fd.FrameCount && c.Target == fd.Target))
                            {
                                clip.Clip.Channels.Add(fd);
                            }
                        }

                        SaveClip(clip, t.Item3);

                        posePack.RemoveAnimation();

                        Console.WriteLine(clip.ClipName);
                    }
                }





                //posePack.ImportAnimation(@"M:\Sims 4 Infant Clip Exports\Adult\a_loco_crawl_bank_left_pose_x.blend");

                package.Save();
                //IResourceProvider GlobalSource = AppModel.Instance.GlobalFiles;

                //FiletableAnimationListProvider alp = new FiletableAnimationListProvider();



                return;
            }

            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                if (!args[0].StartsWith("-"))
                {
                    var originalDir = args[0];
                    var modDir = args[1];
                    var prefix = args[2];
                    var targetDir = args[3];

                    foreach (var file in Directory.GetFiles(modDir))
                    {
                        var poseClipName = file.Split('.')[1].Split('.')[0];
                        var clipName = Regex.Match(File.ReadAllText(file), "([a-zA-Z0-9_-]+).blend").Groups[1].Value;
                        var origClipName = clipName.Split(prefix)[1]/*.Replace("p_loco_tier02","p_loco").Replace("p_loco","a_loco_default")*/;
                        var origToddlerClipName = clipName.Split(prefix)[1];


                        var hash = FNV.Hash64(clipName);
                        var hashHexStr = $"{hash:X}";
                        if (fileLookup.ContainsKey(origClipName))
                        {
                            var origClip = fileLookup[origClipName];

                            var cr = new ClipResource();
                            using (var fs = new FileStream(origClip, FileMode.Open))
                            {
                                cr.Read(fs);
                            }

                            var cr2 = new ClipResource();
                            using (var fs = new FileStream(file, FileMode.Open))
                            {
                                var ch = cr.Clip.Channels.Where(s => s.Target == 720414894u).ToList();

                                cr2.Read(fs);
                                cr2.ClipName = clipName;
                                cr2.Clip.Name = clipName;
                                cr2.Clip.F1Palette = cr.Clip.F1Palette;

                                var translationChannel = ch.First(a => a.ChannelSubTarget == SubTargetType.Translation);
                                var orientationChannel = ch.First(a => a.ChannelSubTarget == SubTargetType.Orientation);

                                var newTranslationChannel = new Channel();
                                newTranslationChannel.Target = translationChannel.Target;
                                newTranslationChannel.Offset = translationChannel.Offset;
                                newTranslationChannel.FrameCount = translationChannel.FrameCount;
                                newTranslationChannel.ChannelType = translationChannel.ChannelType;
                                newTranslationChannel.ChannelSubTarget = translationChannel.ChannelSubTarget;
                                newTranslationChannel.Scale = translationChannel.Scale;
                                newTranslationChannel.FrameData = translationChannel.FrameData.ToList().ToArray();

                                var newOrientationChannel = new Channel();
                                newOrientationChannel.Target = orientationChannel.Target;
                                newOrientationChannel.Offset = orientationChannel.Offset;
                                newOrientationChannel.FrameCount = orientationChannel.FrameCount;
                                newOrientationChannel.ChannelType = orientationChannel.ChannelType;
                                newOrientationChannel.ChannelSubTarget = orientationChannel.ChannelSubTarget;
                                newOrientationChannel.Scale = orientationChannel.Scale;
                                newOrientationChannel.FrameData = orientationChannel.FrameData.ToList().ToArray();


                                cr2.Clip.Channels.Insert(0, newOrientationChannel);
                                cr2.Clip.Channels.Insert(1, newTranslationChannel);

                            

                            
                                if (cr2.Clip.TickCount < translationChannel.FrameCount)
                                {
                                    var frameBytes = translationChannel.FrameData.Length / translationChannel.FrameCount;

                                    newTranslationChannel.FrameCount = cr2.Clip.TickCount;
                                    //newTranslationChannel.FrameData = translationChannel.FrameData.Take(translationChannel.FrameData.Length - (frameBytes * (translationChannel.FrameCount - cr2.Clip.TickCount))).ToArray();

                                }
                                //else if (cr2.Clip.TickCount > translationChannel.FrameCount && translationChannel.FrameCount > 0)
                                //{
                                //    var frameBytes = translationChannel.FrameData.Length / translationChannel.FrameCount;

                                //    Console.WriteLine(frameBytes);

                                //    newTranslationChannel.FrameCount = cr2.Clip.TickCount;

                                //    if(translationChannel.FrameCount > 1)
                                //    {
                                    
                                //        var set1 = translationChannel.FrameData.TakeLast(20).Take(10).ToArray();
                                //        var set2 = translationChannel.FrameData.TakeLast(10).ToArray();
                                //        Console.WriteLine(translationChannel.FrameData.Length);
                                //        Console.WriteLine(set1.Length);
                                //        Console.WriteLine(set2.Length);


                                //        Dictionary<int, int> frameDataIncrements = new Dictionary<int, int>();

                                //        for (var i = 0; i < frameBytes; i++)
                                //        {

                                //            Console.WriteLine($"{set1[i]},{set2[i]}");

                                //            frameDataIncrements.Add(i, (set2[i] - set1[i]));
                                //        }

                                //        var tempFrameData = translationChannel.FrameData.ToList();

                                //        for (var i = translationChannel.FrameCount; i < newTranslationChannel.FrameCount - 1; i++)
                                //        {
                                //            for(var j = 0; j < frameBytes; j++)
                                //            {
                                //                tempFrameData.Add((byte)(tempFrameData[tempFrameData.Count - (frameBytes)] + frameDataIncrements[j]));
                                //            }
                                //        }
                                //        newTranslationChannel.FrameData = tempFrameData.ToArray();
                                //    }
                                //}
                            }

                            var newFileName = Path.Join(targetDir, $"6B20C4F3!00000000!{hashHexStr}.{clipName}.Clip");

                            using (var fs = new FileStream(newFileName, FileMode.OpenOrCreate))
                            {
                                cr2.Write(fs);
                            }

                            var headerFileName = Path.Join(targetDir, $"BC4A5044!00000000!{hashHexStr}.{clipName}.ClipHeader");

                            using (var fs = new FileStream(headerFileName, FileMode.OpenOrCreate))
                            {
                                new ClipHeaderResource(cr2).Write(fs);
                            }

                            Console.WriteLine($"{poseClipName},{clipName}");
                        }
                    }
                }
                else
                {
                    if(o.Mode == "" || o.Mode == null)
                    {
                        o.Mode = args[0];
                    }
                    switch(o.Mode)
                    {
                        case "-s":
                            var cr = new ClipResource();
                            using (var fs = new FileStream(args[1], FileMode.Open))
                            {
                                cr.Read(fs);
                            }

                            var clipName = Regex.Match(File.ReadAllText(args[2]), "([a-zA-Z0-9_-]+).blend").Groups[1].Value;

                            var hash = FNV.Hash64(clipName);
                            var hashHexStr = $"{hash:X}";

                            var cr2 = new ClipResource();
                            using (var fs = new FileStream(args[2], FileMode.Open))
                            {
                                var ch = cr.Clip.Channels.Where(s => s.Target == 720414894u).ToList();

                                cr2.Read(fs);
                                cr2.ClipName = clipName;
                                cr2.Clip.Name = clipName;
                                cr2.Clip.F1Palette = cr.Clip.F1Palette;

                                var translationChannel = ch.First(a => a.ChannelSubTarget == SubTargetType.Translation);
                                var orientationChannel = ch.First(a => a.ChannelSubTarget == SubTargetType.Orientation);

                                var newTranslationChannel = new Channel();
                                newTranslationChannel.Target = translationChannel.Target;
                                newTranslationChannel.Offset = translationChannel.Offset;
                                newTranslationChannel.FrameCount = translationChannel.FrameCount;
                                newTranslationChannel.ChannelType = translationChannel.ChannelType;
                                newTranslationChannel.ChannelSubTarget = translationChannel.ChannelSubTarget;
                                newTranslationChannel.Scale = translationChannel.Scale;
                                newTranslationChannel.FrameData = translationChannel.FrameData.ToList().ToArray();

                                var newOrientationChannel = new Channel();
                                newOrientationChannel.Target = orientationChannel.Target;
                                newOrientationChannel.Offset = orientationChannel.Offset;
                                newOrientationChannel.FrameCount = orientationChannel.FrameCount;
                                newOrientationChannel.ChannelType = orientationChannel.ChannelType;
                                newOrientationChannel.ChannelSubTarget = orientationChannel.ChannelSubTarget;
                                newOrientationChannel.Scale = orientationChannel.Scale;
                                newOrientationChannel.FrameData = orientationChannel.FrameData.ToList().ToArray();


                                cr2.Clip.Channels.Insert(0, newOrientationChannel);
                                cr2.Clip.Channels.Insert(1, newTranslationChannel);

                                if (cr2.Clip.TickCount < translationChannel.FrameCount)
                                {
                                    newTranslationChannel.FrameCount = cr2.Clip.TickCount;
                                    //newTranslationChannel.FrameData = translationChannel.FrameData.Take(translationChannel.FrameData.Length - (10 * (translationChannel.FrameCount - cr2.Clip.TickCount))).ToArray();
                                }
                                //else if (cr2.Clip.TickCount > translationChannel.FrameCount && translationChannel.FrameCount > 0)
                                //{
                                //    var newFrameData = new List<byte>();

                                //    for (int i = 0; i < cr2.Clip.TickCount; i++)
                                //    {
                                //        for (int j = 0; j < 10; j++)
                                //        {
                                //            switch (j)
                                //            {
                                //                case 0:
                                //                case 8:
                                //                    newFrameData.Add((byte)i);
                                //                    break;
                                //                case 2:
                                //                    if (i == 0)
                                //                        newFrameData.Add(16);
                                //                    else
                                //                        newFrameData.Add(0);
                                //                    break;
                                //                default:
                                //                    newFrameData.Add(0);
                                //                    break;
                                //            }
                                //        }
                                //    }
                                //    newTranslationChannel.FrameCount = cr2.Clip.TickCount;
                                //    newTranslationChannel.FrameData = newFrameData.ToArray();
                                //}
                            }

                            var newFileName = Path.Join(args[3], $"6B20C4F3!00000000!{hashHexStr}.{clipName}.Clip");

                            using (var fs = new FileStream(newFileName, FileMode.OpenOrCreate))
                            {
                                cr2.Write(fs);
                            }

                            var headerFileName = Path.Join(args[3], $"BC4A5044!00000000!{hashHexStr}.{clipName}.ClipHeader");

                            using (var fs = new FileStream(headerFileName, FileMode.OpenOrCreate))
                            {
                                new ClipHeaderResource(cr2).Write(fs);
                            }
                            break;

                        case "backcarrier":
                            var originalDir = args[2];
                            var modDir = args[3];
                            var targetDir = args[4];


                            foreach (var file in Directory.GetFiles(modDir))
                            {
                                //var poseClipName = file.Split('.')[1].Split('.')[0];
                                //clipName = Regex.Match(File.ReadAllText(file), "([a-zA-Z0-9_-]+).blend").Groups[1].Value;




                                cr = new ClipResource();
                                using (var fs = new FileStream(file, FileMode.Open))
                                {
                                    cr.Read(fs);
                                }


                                var newClipName = cr.ClipName;


                                if (newClipName.StartsWith("_"))
                                {
                                    newClipName = "a" + newClipName;
                                }
                                else if(newClipName.StartsWith("i_"))
                                {
                                    newClipName = "a" + newClipName.Substring(1);
                                }
                                else if (newClipName.StartsWith("a_") || newClipName.StartsWith("c_"))
                                {
                                    var origClipName = "i" + newClipName.Substring(1);

                                    if (fileLookup.ContainsKey(origClipName))
                                    {
                                        Console.WriteLine(origClipName);
                                        var origCr = new ClipResource();
                                        using (var fs = new FileStream(fileLookup[origClipName], FileMode.Open))
                                        {
                                            origCr.Read(fs);
                                        }

                                        foreach (var fd in origCr.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation))
                                        {
                                            if (!cr.Clip.Channels.Any(c => c.ChannelSubTarget == fd.ChannelSubTarget && c.FrameCount == fd.FrameCount && c.Target == fd.Target))
                                            {
                                                cr.Clip.Channels.Add(fd);
                                            }
                                        }

                                        cr.Duration = origCr.Duration;
                                        cr.Clip.TickCount = origCr.Clip.TickCount;

                                    }
                                }
                                else if (newClipName.StartsWith("a2a_") || newClipName.StartsWith("c2c_") || newClipName.StartsWith("a2c_"))
                                {
                                    var origClipName = "a2i" + newClipName.Substring(3);

                                    if (!fileLookup.ContainsKey(origClipName))
                                    {
                                        origClipName = "i2i" + newClipName.Substring(3);
                                    }

                                    if (fileLookup.ContainsKey(origClipName))
                                    {
                                        Console.WriteLine(origClipName);
                                        var origCr = new ClipResource();
                                        using (var fs = new FileStream(fileLookup[origClipName], FileMode.Open))
                                        {
                                            origCr.Read(fs);
                                        }


                                        foreach (var fd in origCr.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation))
                                        {
                                            if (!cr.Clip.Channels.Any(c => c.ChannelSubTarget == fd.ChannelSubTarget && c.FrameCount == fd.FrameCount && c.Target == fd.Target))
                                            {
                                                cr.Clip.Channels.Add(fd);
                                            }
                                        }

                                        cr.Duration = origCr.Duration;
                                        cr.Clip.TickCount = origCr.Clip.TickCount;

                                    }
                                }



                                cr.ClipName = newClipName;
                                cr.Clip.Name = newClipName;

                                SaveClip(cr, targetDir);


                                //hash = FNV.Hash64(newClipName);
                                //hashHexStr = $"{hash:X}";

                                //newFileName = Path.Join(targetDir, $"6B20C4F3!00000000!{hashHexStr}.{newClipName}.Clip");

                                //using (var fs = new FileStream(newFileName, FileMode.OpenOrCreate))
                                //{
                                //    cr.Write(fs);
                                //}

                                //headerFileName = Path.Join(targetDir, $"BC4A5044!00000000!{hashHexStr}.{newClipName}.ClipHeader");

                                //using (var fs = new FileStream(headerFileName, FileMode.OpenOrCreate))
                                //{
                                //    new ClipHeaderResource(cr).Write(fs);
                                //}

                                //Console.WriteLine($"{poseClipName},{newClipName}");
                            }
                            break;

                        case "carryOld":
                            originalDir = args[2];
                            modDir = args[3];
                            targetDir = args[4];

                            foreach (var file in Directory.GetFiles(modDir))
                            {
                                //var poseClipName = file.Split('.')[1].Split('.')[0];
                                //clipName = Regex.Match(File.ReadAllText(file), "([a-zA-Z0-9_-]+).blend").Groups[1].Value;


                                clipName = Regex.Match(File.ReadAllText(file), "([a-zA-Z0-9_-]+).blend").Groups[1].Value;

                                if(clipName.StartsWith("adult_a2p"))
                                {
                                    clipName = clipName.Replace("adult_a2p", "a2a");
                                }
                                if (clipName.StartsWith("child_a2p"))
                                {
                                    clipName = clipName.Replace("child_a2p", "a2c");
                                }

                                cr = new ClipResource();
                                using (var fs = new FileStream(file, FileMode.Open))
                                {
                                    cr.Read(fs);
                                }


                                var newClipName = cr.ClipName;

                                if (newClipName.StartsWith("LilNinthel:PosePack"))
                                {
                                    newClipName = clipName;
                                }


                                var origCr = new ClipResource();

                                var origClipName = "";



                                if (newClipName.StartsWith("a_") || newClipName.StartsWith("c_"))
                                {
                                    origClipName = "p" + newClipName.Substring(1);
                                }
                                else if (newClipName.StartsWith("a2a_") || newClipName.StartsWith("c2c_"))
                                {
                                    origClipName = "a2p" + newClipName.Substring(3);

                                    if (!fileLookup.ContainsKey(origClipName))
                                    {
                                        origClipName = "p2p" + newClipName.Substring(3);
                                    }
                                    if (!fileLookup.ContainsKey(origClipName))
                                    {
                                        origClipName = "p2a" + newClipName.Substring(3);
                                    }
                                }
                                else if (newClipName.StartsWith("a2c_"))
                                {
                                    origClipName = "a2p" + newClipName.Substring(3);
                                }
                                else if (newClipName.StartsWith("c2a_"))
                                {
                                    origClipName = "p2a" + newClipName.Substring(3);
                                }

                                Console.WriteLine(origClipName);

                                if (fileLookup.ContainsKey(origClipName))
                                {
                                    Console.WriteLine(origClipName);

                                    using (var fs = new FileStream(fileLookup[origClipName], FileMode.Open))
                                    {
                                        origCr.Read(fs);
                                    }

                                    foreach (var fd in origCr.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation))
                                    {
                                        if (!cr.Clip.Channels.Any(c => c.ChannelSubTarget == fd.ChannelSubTarget && c.FrameCount == fd.FrameCount && c.Target == fd.Target))
                                        {
                                            cr.Clip.Channels.Add(fd);
                                        }
                                    }

                                    cr.Duration = origCr.Duration;
                                    cr.Clip.TickCount = origCr.Clip.TickCount;

                                }

                                cr.ClipName = newClipName;
                                cr.Clip.Name = newClipName;


                                hash = FNV.Hash64(newClipName);
                                hashHexStr = $"{hash:X}";

                                newFileName = Path.Join(targetDir, $"6B20C4F3!00000000!{hashHexStr}.{newClipName}.Clip");

                                using (var fs = new FileStream(newFileName, FileMode.OpenOrCreate))
                                {
                                    cr.Write(fs);
                                }

                                headerFileName = Path.Join(targetDir, $"BC4A5044!00000000!{hashHexStr}.{newClipName}.ClipHeader");

                                using (var fs = new FileStream(headerFileName, FileMode.OpenOrCreate))
                                {
                                    new ClipHeaderResource(cr).Write(fs);
                                }

                                //Console.WriteLine($"{poseClipName},{newClipName}");
                            }
                            break;
                        case "standing":

                            var package = new DBPFPackage(@"G:\Modding\PC\Sims 4 Mods\Projects\IwnBedwetting Extended Plus\Workspace\DummyPosePack.package");
                            originalDir = args[2];
                            modDir = args[3];
                            targetDir = args[4];
                            string originalAgePrefix = args[4];

                            foreach (var file in Directory.GetFiles(modDir, "*.blend"))
                            {
                                var origClipName = getOriginalClipName(Path.GetFileName(file).Split(".blend")[0], originalAgePrefix, fileLookup);
                                if (origClipName == null)
                                {
                                    Console.WriteLine($"Cannot find original clip for {file}");
                                    continue;
                                }

                                var originalClip = LoadClip(fileLookup[origClipName]);

                           


                                package.Add(originalClip);
                                package.Add(new ClipHeaderResource(originalClip));
                                var posePack = new CustomAnimationPackCustomContent(null, GlobalSource, package, alp);
                                posePack.SelectedAnimation = posePack.Animations.Last();

                                posePack.ImportAnimation(file);

                                originalClip = LoadClip(fileLookup[origClipName]);

                                var clip = posePack.Animations.Last().Clip;

                                clip.Clip.F1Palette = originalClip.Clip.F1Palette;
                                clip.Duration = originalClip.Duration;
                                clip.Clip.TickCount = originalClip.Clip.TickCount;
                                clip.ClipName = Path.GetFileName(file).Split(".blend")[0];
                                clip.Clip.Name = Path.GetFileName(file).Split(".blend")[0];


                                var clipAgeTarget = parseClipActorAgePrefix(clip.ClipName);

                                var standingPostureClip = LoadClip(fileLookup[clipAgeTarget+"_stand_posture_x"]);

                                foreach (var slot in clip.SlotAssignments)
                                {
                                    if (ikTargetReplace.Contains(slot.TargetName))
                                    {
                                        switch (clipAgeTarget)
                                        {
                                            case "a":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_");
                                                break;
                                            case "c":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_child_");
                                                break;
                                        }
                                    }
                                }

                                foreach (var fd in originalClip.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation && c.ChannelSubTarget != SubTargetType.IK_TargetOffset_Translation_World))
                                {
                                    if (!clip.Clip.Channels.Any(c => c.ChannelSubTarget == fd.ChannelSubTarget && c.FrameCount == fd.FrameCount && c.Target == fd.Target))
                                    {
                                        clip.Clip.Channels.Add(fd);
                                    }
                                }

                                var standingIK = standingPostureClip.Clip.Channels.Where(c => c.ChannelSubTarget == SubTargetType.IK_TargetOffset_Translation_World && BoneHashes.bothFeet.Contains(c.Target));

                                RemoveUnwantedIK(clip, BoneHashes.bothArmsAndHands);
                                RemoveUnwantedIK(clip, BoneHashes.root);

                                foreach(var ikChannel in standingIK)
                                {
                                    clip.Clip.Channels.Add(ikChannel);
                                }
                            

                                SaveClip(clip, targetDir);

                                posePack.RemoveAnimation();

                                Console.WriteLine(clip.ClipName);
                            }
                            break;
                        case "tummyTime":
                            Console.WriteLine("tummyTime");
                            package = new DBPFPackage(@"G:\Modding\PC\Sims 4 Mods\Projects\IwnBedwetting Extended Plus\Workspace\DummyPosePack.package");
                            originalDir = args[2];
                            modDir = args[3];
                            targetDir = args[4];
                            originalAgePrefix = args[5];

                            foreach (var file in Directory.GetFiles(modDir, "*.blend"))
                            {
                                var origClipName = getOriginalClipName(Path.GetFileName(file).Split(".blend")[0], originalAgePrefix, fileLookup);
                                if (origClipName == null)
                                {
                                    Console.WriteLine($"Cannot find original clip for {file}");
                                    continue;
                                }

                                var originalClip = LoadClip(fileLookup[origClipName]);

                                package.Add(originalClip);
                                package.Add(new ClipHeaderResource(originalClip));
                                var posePack = new CustomAnimationPackCustomContent(null, GlobalSource, package, alp);
                                posePack.SelectedAnimation = posePack.Animations.Last();

                                posePack.ImportAnimation(file);

                                originalClip = LoadClip(fileLookup[origClipName]);

                                var clip = posePack.Animations.Last().Clip;

                                clip.Clip.F1Palette = originalClip.Clip.F1Palette;
                                clip.Duration = originalClip.Duration;
                                clip.Clip.TickCount = originalClip.Clip.TickCount;
                                clip.ClipName = Path.GetFileName(file).Split(".blend")[0];
                                clip.Clip.Name = Path.GetFileName(file).Split(".blend")[0];


                                var clipAgeTarget = parseClipActorAgePrefix(clip.ClipName);

                                foreach (var slot in clip.SlotAssignments)
                                {
                                    if (ikTargetReplace.Contains(slot.TargetName))
                                    {
                                        switch (clipAgeTarget)
                                        {
                                            case "a":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_");
                                                break;
                                            case "c":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_child_");
                                                break;
                                        }
                                    }
                                }

                                foreach (var fd in originalClip.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation))
                                {
                                    if (!clip.Clip.Channels.Any(c => c.ChannelSubTarget == fd.ChannelSubTarget && c.FrameCount == fd.FrameCount && c.Target == fd.Target))
                                    {
                                        clip.Clip.Channels.Add(fd);
                                    }
                                }

                                RemoveUnwantedIK(clip, BoneHashes.bothArmsAndHands);
                                RemoveUnwantedIK(clip, BoneHashes.bothLegsAndFeet);


                                SaveClip(clip, targetDir);

                                posePack.RemoveAnimation();

                                Console.WriteLine(clip.ClipName);
                            }
                            break;
                        case "seated":
                            package = new DBPFPackage(@"G:\Modding\PC\Sims 4 Mods\Projects\IwnBedwetting Extended Plus\Workspace\DummyPosePack.package");
                            originalDir = args[2];
                            modDir = args[3];
                            targetDir = args[4];
                            originalAgePrefix = args[5];

                            foreach (var file in Directory.GetFiles(modDir, "*.blend"))
                            {
                                var origClipName = getOriginalClipName(Path.GetFileName(file).Split(".blend")[0], originalAgePrefix, fileLookup);
                                if (origClipName == null)
                                {
                                    Console.WriteLine($"Cannot find original clip for {file}");
                                    continue;
                                }

                                var originalClip = LoadClip(fileLookup[origClipName]);


                                package.Add(originalClip);
                                package.Add(new ClipHeaderResource(originalClip));
                                var posePack = new CustomAnimationPackCustomContent(null, GlobalSource, package, alp);
                                posePack.SelectedAnimation = posePack.Animations.Last();

                                posePack.ImportAnimation(file);

                                originalClip = LoadClip(fileLookup[origClipName]);

                                var clip = posePack.Animations.Last().Clip;

                                clip.Clip.F1Palette = originalClip.Clip.F1Palette;
                                clip.Duration = originalClip.Duration;
                                clip.Clip.TickCount = originalClip.Clip.TickCount;
                                clip.ClipName = Path.GetFileName(file).Split(".blend")[0];
                                clip.Clip.Name = Path.GetFileName(file).Split(".blend")[0];


                                var clipAgeTarget = parseClipActorAgePrefix(clip.ClipName);


                                foreach (var slot in clip.SlotAssignments)
                                {
                                    if (ikTargetReplace.Contains(slot.TargetName))
                                    {
                                        switch (clipAgeTarget)
                                        {
                                            case "a":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_");
                                                break;
                                            case "c":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_child_");
                                                break;
                                        }
                                    }
                                }

                                foreach (var fd in originalClip.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation))
                                {
                                    if (!clip.Clip.Channels.Any(c => c.ChannelSubTarget == fd.ChannelSubTarget && c.FrameCount == fd.FrameCount && c.Target == fd.Target))
                                    {
                                        clip.Clip.Channels.Add(fd);
                                    }
                                }


                                RemoveUnwantedIK(clip, BoneHashes.bothArmsAndHands);
                                RemoveUnwantedIK(clip, BoneHashes.bothLegsAndFeet);

                                SaveClip(clip, targetDir);

                                posePack.RemoveAnimation();

                                Console.WriteLine(clip.ClipName);
                            }
                            break;
                        case "highChair":
                            package = new DBPFPackage(@"G:\Modding\PC\Sims 4 Mods\Projects\IwnBedwetting Extended Plus\Workspace\DummyPosePack.package");
                            originalDir = args[2];
                            modDir = args[3];
                            targetDir = args[4];
                            originalAgePrefix = args[5];

                            foreach (var file in Directory.GetFiles(modDir, "*.blend"))
                            {
                                var origClipName = getOriginalClipName(Path.GetFileName(file).Split(".blend")[0], originalAgePrefix, fileLookup);
                                if (origClipName == null)
                                {
                                    Console.WriteLine($"Cannot find original clip for {file}");
                                    continue;
                                }

                                var originalClip = LoadClip(fileLookup[origClipName]);


                                package.Add(originalClip);
                                package.Add(new ClipHeaderResource(originalClip));
                                var posePack = new CustomAnimationPackCustomContent(null, GlobalSource, package, alp);
                                posePack.SelectedAnimation = posePack.Animations.Last();

                                posePack.ImportAnimation(file);

                                originalClip = LoadClip(fileLookup[origClipName]);

                                var clip = posePack.Animations.Last().Clip;

                                clip.Clip.F1Palette = originalClip.Clip.F1Palette;
                                clip.Duration = originalClip.Duration;
                                clip.Clip.TickCount = originalClip.Clip.TickCount;
                                clip.ClipName = Path.GetFileName(file).Split(".blend")[0];
                                clip.Clip.Name = Path.GetFileName(file).Split(".blend")[0];


                                var clipAgeTarget = parseClipActorAgePrefix(clip.ClipName);


                                foreach (var slot in clip.SlotAssignments)
                                {
                                    if (ikTargetReplace.Contains(slot.TargetName))
                                    {
                                        switch (clipAgeTarget)
                                        {
                                            case "a":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_");
                                                break;
                                            case "c":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_child_");
                                                break;
                                        }
                                    }
                                }

                                foreach (var fd in originalClip.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation))
                                {
                                    if (!clip.Clip.Channels.Any(c => c.ChannelSubTarget == fd.ChannelSubTarget && c.FrameCount == fd.FrameCount && c.Target == fd.Target))
                                    {
                                        clip.Clip.Channels.Add(fd);
                                    }
                                }


                                RemoveUnwantedIK(clip, BoneHashes.bothArmsAndHands);
                                RemoveUnwantedIK(clip, BoneHashes.bothLegsAndFeet);

                                SaveClip(clip, targetDir);

                                posePack.RemoveAnimation();

                                Console.WriteLine(clip.ClipName);
                            }
                            break;
                        case "carry":
                            package = new DBPFPackage(@"G:\Modding\PC\Sims 4 Mods\Projects\IwnBedwetting Extended Plus\Workspace\DummyPosePack.package");
                            originalDir = args[2];
                            modDir = args[3];
                            targetDir = args[4];
                            originalAgePrefix = args[5];

                            foreach (var file in Directory.GetFiles(modDir, "*.blend"))
                            {
                                var origClipName = getOriginalClipName(Path.GetFileName(file).Split(".blend")[0], originalAgePrefix, fileLookup);
                                if (origClipName == null)
                                {
                                    Console.WriteLine($"Cannot find original clip for {file}");
                                    continue;
                                }

                                var originalClip = LoadClip(fileLookup[origClipName]);


                                package.Add(originalClip);
                                package.Add(new ClipHeaderResource(originalClip));
                                var posePack = new CustomAnimationPackCustomContent(null, GlobalSource, package, alp);
                                posePack.SelectedAnimation = posePack.Animations.Last();

                                posePack.ImportAnimation(file);

                                originalClip = LoadClip(fileLookup[origClipName]);

                                var clip = posePack.Animations.Last().Clip;

                                clip.Clip.F1Palette = originalClip.Clip.F1Palette;
                                clip.Duration = originalClip.Duration;
                                clip.Clip.TickCount = originalClip.Clip.TickCount;
                                clip.ClipName = Path.GetFileName(file).Split(".blend")[0];
                                clip.Clip.Name = Path.GetFileName(file).Split(".blend")[0];


                                var clipAgeTarget = parseClipActorAgePrefix(clip.ClipName);


                                foreach (var slot in clip.SlotAssignments)
                                {
                                    if (ikTargetReplace.Contains(slot.TargetName))
                                    {
                                        switch (clipAgeTarget)
                                        {
                                            case "a":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_");
                                                break;
                                            case "c":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_child_");
                                                break;
                                        }
                                    }
                                }

                                foreach (var fd in originalClip.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation))
                                {
                                    if (!clip.Clip.Channels.Any(c => c.ChannelSubTarget == fd.ChannelSubTarget && c.FrameCount == fd.FrameCount && c.Target == fd.Target))
                                    {
                                        clip.Clip.Channels.Add(fd);
                                    }
                                }


                                //RemoveUnwantedIK(clip, BoneHashes.bothArmsAndHands);
                                //RemoveUnwantedIK(clip, BoneHashes.bothLegsAndFeet);
                                //RemoveUnwantedIK(clip, BoneHashes.bothLegsAndFeet);

                                SaveClip(clip, targetDir);

                                posePack.RemoveAnimation();

                                Console.WriteLine(clip.ClipName);
                            }
                            break;
                        case "bath":
                            package = new DBPFPackage(@"G:\Modding\PC\Sims 4 Mods\Projects\IwnBedwetting Extended Plus\Workspace\DummyPosePack.package");
                            originalDir = args[2];
                            modDir = args[3];
                            targetDir = args[4];
                            originalAgePrefix = args[5];

                            foreach (var file in Directory.GetFiles(modDir, "*.blend"))
                            {
                                var origClipName = getOriginalClipName(Path.GetFileName(file).Split(".blend")[0], originalAgePrefix, fileLookup);
                                if (origClipName == null)
                                {
                                    Console.WriteLine($"Cannot find original clip for {file}");
                                    continue;
                                }

                                var originalClip = LoadClip(fileLookup[origClipName]);


                                package.Add(originalClip);
                                package.Add(new ClipHeaderResource(originalClip));
                                var posePack = new CustomAnimationPackCustomContent(null, GlobalSource, package, alp);
                                posePack.SelectedAnimation = posePack.Animations.Last();

                                posePack.ImportAnimation(file);

                                originalClip = LoadClip(fileLookup[origClipName]);

                                var clip = posePack.Animations.Last().Clip;

                                clip.Clip.F1Palette = originalClip.Clip.F1Palette;
                                clip.Duration = originalClip.Duration;
                                clip.Clip.TickCount = originalClip.Clip.TickCount;
                                clip.ClipName = Path.GetFileName(file).Split(".blend")[0];
                                clip.Clip.Name = Path.GetFileName(file).Split(".blend")[0];


                                var clipAgeTarget = parseClipActorAgePrefix(clip.ClipName);


                                foreach (var slot in clip.SlotAssignments)
                                {
                                    if (ikTargetReplace.Contains(slot.TargetName))
                                    {
                                        switch (clipAgeTarget)
                                        {
                                            case "a":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_");
                                                break;
                                            case "c":
                                                slot.TargetName = slot.TargetName.Replace("_infant_", "_toddler_").Replace("_toddler_", "_child_");
                                                break;
                                        }
                                    }
                                }

                                foreach (var fd in originalClip.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation))
                                {
                                    if (!clip.Clip.Channels.Any(c => c.ChannelSubTarget == fd.ChannelSubTarget && c.FrameCount == fd.FrameCount && c.Target == fd.Target))
                                    {
                                        clip.Clip.Channels.Add(fd);
                                    }
                                }


                                RemoveUnwantedIK(clip, BoneHashes.bothArmsAndHands);
                                RemoveUnwantedIK(clip, BoneHashes.bothLegsAndFeet);

                                SaveClip(clip, targetDir);

                                posePack.RemoveAnimation();

                                Console.WriteLine(clip.ClipName);
                            }
                            break;
                        case "diaperPackageMaker":
                            UniversalDiaperCASPackager(o, GlobalSource, blender_util); 
                            break;
                        case "breastfeed":
                            originalDir = args[2];
                            modDir = args[3];
                            targetDir = args[4];

                            fileLookup = new Dictionary<String, String>();

                            foreach (var file in Directory.GetFiles(originalDir))
                            {
                                clipName = file.Split('.')[1].Split('.')[0];

                                fileLookup.Add(clipName, file);
                            }

                            foreach (var file in Directory.GetFiles(modDir))
                            {
                                //var poseClipName = file.Split('.')[1].Split('.')[0];
                                //clipName = Regex.Match(File.ReadAllText(file), "([a-zA-Z0-9_-]+).blend").Groups[1].Value;


                                clipName = Regex.Match(File.ReadAllText(file), "([a-zA-Z0-9_-]+).blend").Groups[1].Value;

                                if (clipName.StartsWith("adult_a2p"))
                                {
                                    clipName = clipName.Replace("adult_a2p", "a2a");
                                }
                                if (clipName.StartsWith("child_a2p"))
                                {
                                    clipName = clipName.Replace("child_a2p", "a2c");
                                }

                                cr = new ClipResource();
                                using (var fs = new FileStream(file, FileMode.Open))
                                {
                                    cr.Read(fs);
                                }


                                var newClipName = cr.ClipName;

                                if (newClipName.StartsWith("LilNinthel:PosePack"))
                                {
                                    newClipName = clipName;
                                }


                                var origCr = new ClipResource();

                                var origClipName = "";



                                if (newClipName.StartsWith("a_") || newClipName.StartsWith("c_"))
                                {
                                    origClipName = "i" + newClipName.Substring(1);
                                }
                                else if (newClipName.StartsWith("a2a_") || newClipName.StartsWith("c2c_"))
                                {
                                    origClipName = "a2i" + newClipName.Substring(3);

                                    if (!fileLookup.ContainsKey(origClipName))
                                    {
                                        origClipName = "i2i" + newClipName.Substring(3);
                                    }
                                    if (!fileLookup.ContainsKey(origClipName))
                                    {
                                        origClipName = "i2a" + newClipName.Substring(3);
                                    }
                                }
                                else if (newClipName.StartsWith("a2c_"))
                                {
                                    origClipName = "a2i" + newClipName.Substring(3);
                                }
                                else if (newClipName.StartsWith("c2a_"))
                                {
                                    origClipName = "i2a" + newClipName.Substring(3);
                                }

                                //Console.WriteLine(origClipName);

                                if (fileLookup.ContainsKey(origClipName))
                                {
                                    Console.WriteLine(newClipName);

                                    using (var fs = new FileStream(fileLookup[origClipName], FileMode.Open))
                                    {
                                        origCr.Read(fs);
                                    }

                                    foreach (var fd in origCr.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation))
                                    {
                                        if (!cr.Clip.Channels.Any(c => c.ChannelSubTarget == fd.ChannelSubTarget && c.FrameCount == fd.FrameCount && c.Target == fd.Target))
                                        {
                                            cr.Clip.Channels.Add(fd);
                                        }
                                    }

                                    cr.Duration = origCr.Duration;
                                    cr.Clip.TickCount = origCr.Clip.TickCount;

                                }

                                cr.ClipName = newClipName;
                                cr.Clip.Name = newClipName;

                                hash = FNV.Hash64(newClipName);
                                hashHexStr = $"{hash:X}";

                                newFileName = Path.Join(targetDir, $"6B20C4F3!00000000!{hashHexStr}.{newClipName}.Clip");

                                using (var fs = new FileStream(newFileName, FileMode.OpenOrCreate))
                                {
                                    cr.Write(fs);
                                }

                                headerFileName = Path.Join(targetDir, $"BC4A5044!00000000!{hashHexStr}.{newClipName}.ClipHeader");

                                using (var fs = new FileStream(headerFileName, FileMode.OpenOrCreate))
                                {
                                    new ClipHeaderResource(cr).Write(fs);
                                }

                                //Console.WriteLine($"{poseClipName},{newClipName}");
                            }
                            break;
                    }
                }

            });

            //Console.WriteLine(FNV.Hash32(args[0]));
            //Console.WriteLine(FNV.Hash64(args[0]));




            //var cr = new ClipResource();
            //using (var fs = new FileStream(@"G:\Modding\PC\Sims 4 Mods\Projects\ABDL Waddle\6B20C4F3!00000000!C16D97734CCD6477.a_loco_default_walk_RFoot_short_x.Clip", FileMode.Open))
            //{
            //    cr.Read(fs);

            //}

            //Console.WriteLine(cr.Json);
        }

        private static void UniversalDiaperCASPackager(Options options, IResourceProvider GlobalSource, BlenderUtilities blender_util)
        {
            ulong modelId = 0;
            bool needToImportModel = false;
            ulong normalId = 0;
            bool needToImportNormal = false;
            ulong specularId = 0;
            bool needToImportSpecular = false;


            bool needToFindModel = false;
            bool needToFindSpecular = false;
            bool needToFindNormal = false;

            bool needToParseConfig = true;

            string textureDir = options.DiaperTextureRoot;

            if (!Directory.Exists(textureDir))
            {
                throw new Exception("Texture directory does not exist");
            }



            if (options.ModelUseExisting)
            {
                needToFindModel = true;
            }
            else
            {
                if (File.Exists(options.ModelFile))
                {
                    needToImportModel = true;
                }
                else
                {
                    try
                    {
                        modelId = System.Convert.ToUInt64(options.ModelId, 16);
                    }
                    catch
                    {
                        modelId = ulong.Parse(options.ModelId);
                    }
                }
            }


            if (options.SpecularUseExisting)
            {
                needToFindSpecular = true;
            }
            else
            {
                if (File.Exists(options.SpecularFile))
                {
                    needToImportSpecular = true;
                }
                else
                {
                    try
                    {
                        specularId = System.Convert.ToUInt64(options.SpecularId, 16);
                    }
                    catch
                    {
                        specularId = ulong.Parse(options.SpecularId);
                    }
                }
            }


            if (options.NormalMapUseExisting)
            {
                needToFindNormal = true;
            }
            else
            {
                if (File.Exists(options.NormalMapFile))
                {
                    needToImportNormal = true;
                }
                else
                {
                    try
                    {
                        normalId = System.Convert.ToUInt64(options.NormalMapId, 16);
                    }
                    catch
                    {
                        normalId = ulong.Parse(options.NormalMapId);
                    }
                }
            }

            string gender = options.Gender switch
            {
                "f" => "f",
                "m" => "m",
                "female" => "f",
                "male" => "m",
                _ => ""
            };

            if (gender == "")
            {
                throw new Exception("Gender not set correctly");
            }



            string bodyType = options.BodyType switch
            {
                "a" => "accessory",
                "b" => "bottom",
                "accessory" => "accessory",
                "bottom" => "bottom",
                _ => ""
            };

            if (bodyType == "")
            {
                throw new Exception("Body type not set correctly");
            }


            string packageName = options.PackageName;

            if (needToFindModel || needToFindNormal || needToFindSpecular)
            {
                if (!File.Exists(textureDir + "\\" + packageName + ".package"))
                {
                    throw new Exception("Cannot use existing data when package does not exist");
                }
            }

            var visibleStates = new HashSet<Tuple<int, int>>();
            visibleStates.Add(new Tuple<int, int>(0, 0));



            if (options.VisibleStates != null)
            {
                foreach (var state in options.VisibleStates)
                {
                    int wetness = 0;
                    int messiness = 0;
                    var wetMatch = Regex.Match(state, @"w(\d)");
                    var messMatch = Regex.Match(state, @"m(\d)");
                    if (wetMatch.Success)
                    {
                        wetness = int.Parse(wetMatch.Captures[0].Value);
                    }
                    if (messMatch.Success)
                    {
                        messiness = int.Parse(messMatch.Captures[0].Value);
                    }
                    visibleStates.Add(new Tuple<int, int>(wetness, messiness));
                }
            }

            DBPFPackage package = new DBPFPackage(textureDir + "\\" + packageName + ".package");

            uint modelType = RegionMap.Type;
            uint normalMapType = DSTImageResource.Type;
            uint specularType = RLESImageResource.Type;
            uint diffuseType = RLE2ImageResource.Type;

            ulong femaleBase = System.Convert.ToUInt64("1990", 16);
            ulong maleBase = System.Convert.ToUInt64("19AE", 16);

            var femaleResource = GlobalSource.Find<CASPartResource>(femaleBase);
            var femaleRegionMap = GlobalSource.Find<RegionMap>(femaleResource.RegionMap.Instance).Copy();
            //var femaleGeometryList = femaleRegionMap.Entries.First().Models.Select(m => (GeometryResource)GlobalSource.Find(m)).ToList();

            var maleResource = GlobalSource.Find<CASPartResource>(maleBase);
            var maleRegionMap = GlobalSource.Find<RegionMap>(maleResource.RegionMap.Instance).Copy();
            //var maleGeometryList = maleRegionMap.Entries.First().Models.Select(m => (GeometryResource)GlobalSource.Find(m)).ToList();

            var tempRegionMap = gender == "f" ? femaleRegionMap : maleRegionMap;

            ushort secondaryIndex = 0;

            uint auralMaterialHash = 2166136261u;
            int sortLayer = 10300;

            if (needToFindModel)
            {
                var models = package.FindAllByType(modelType);
                if (models.Count() != 1)
                {
                    throw new Exception("Must be exactly 1 RegionMap in package");
                }

                modelId = models.Single().Instance;

                needToFindModel = false;
            }

            if (needToFindSpecular)
            {
                var models = package.FindAllByType(specularType);
                if (models.Count() != 1)
                {
                    throw new Exception("Must be exactly 1 RLES Image in package");
                }

                specularId = models.Single().Instance;

                needToFindSpecular = false;
            }

            if (needToFindNormal)
            {
                var models = package.FindAllByType(normalMapType);
                if (models.Count() != 1)
                {
                    throw new Exception("Must be exactly 1 DST Image in package");
                }

                normalId = models.Single().Instance;

                needToFindNormal = false;
            }

            List<Tag> tags = new List<Tag>{
                            new Tag(TagCategory.Occult, TagValue.Occult_Alien),
                            new Tag(TagCategory.Occult, TagValue.Occult_Human),
                            new Tag(TagCategory.Occult, TagValue.Occult_Vampire),
                            new Tag(TagCategory.Occult, TagValue.Occult_Witch),
                            new Tag(TagCategory.Occult, TagValue.Occult_Human),
                            new Tag(TagCategory.GenderAppropriate, gender == "f" ? TagValue.GenderAppropriate_Female : TagValue.GenderAppropriate_Male),
                            new Tag(TagCategory.Fabric, TagValue.Fabric_Synthetic),
                            new Tag(TagCategory.Color, TagValue.Color_White),
                        };

            if (options.IncludeOutfitCategories)
            {
                tags.AddRange(new List<Tag>
                {
                    new Tag(TagCategory.OutfitCategory, TagValue.OutfitCategory_Athletic),
                    new Tag(TagCategory.OutfitCategory, TagValue.OutfitCategory_Everyday),
                    new Tag(TagCategory.OutfitCategory, TagValue.OutfitCategory_Formal),
                    new Tag(TagCategory.OutfitCategory, TagValue.OutfitCategory_Party),
                    new Tag(TagCategory.OutfitCategory, TagValue.OutfitCategory_Sleep),
                    new Tag(TagCategory.OutfitCategory, TagValue.OutfitCategory_HotWeather),
                    new Tag(TagCategory.OutfitCategory, TagValue.OutfitCategory_ColdWeather),
                });
            }


            BodyType partBodyType = bodyType == "accessory" ? BodyType.IndexFingerLeft : BodyType.LowerBody;

            AgeGenderFlags ageGender = new AgeGenderFlags
            {
                Adult = true,
                Elder = true,
                Teen = true,
                YoungAdult = true,
                Female = gender == "f",
                Male = gender == "m"
            };

            if (partBodyType == BodyType.LowerBody)
            {
                tags.Add(new Tag(TagCategory.Bottom, TagValue.Bottom_Underwear));
            }

            uint group = 2147483648u;

            var localSource = new PackageResourceProvider(package);

            

            Regex matcher = new Regex(@"w([0-6])m([0-4])\.(png|dds)");

            var existingTextures = package.FindAllByType(diffuseType).ToList();
            if(existingTextures.Any())
            {
                foreach (var item in existingTextures)
                {
                    package.RemoveEntry(item);
                }
            }

            var configDictList = new List<Dictionary<Tuple<int, int>, ulong>>();

            //if (needToParseConfig)
            //{
                var configRef = package.FindAllByType(SnippetTuningResource.Type);
                if(!configRef.Any())
                {
                    needToParseConfig = false;
                }
                else
                {
                    var config = localSource.Find<SnippetTuningResource>(configRef.Single().Instance);

                    var configRoot = XElement.Parse(config.XmlText, LoadOptions.PreserveWhitespace);

                    foreach (var diaperConfig in configRoot.Elements("L").Elements("U"))
                    {
                        var configDict = new Dictionary<Tuple<int, int>, ulong>();
                        configDict[new Tuple<int, int>(0, 0)] = ulong.Parse(diaperConfig.Elements("T").Single().Value);

                        foreach (var diaperLoadConfig in diaperConfig.Elements("L").Elements("U"))
                        {
                            var wetness = 0;
                            var messiness = 0;
                            var casPart = 0ul;

                            foreach (var diaperState in diaperLoadConfig.Elements("T"))
                            {
                                switch (diaperState.Attribute("n").Value)
                                {
                                    case "wetness_level":
                                        wetness = int.Parse(diaperState.Value);
                                        break;
                                    case "mess_level":
                                        messiness = int.Parse(diaperState.Value);
                                        break;
                                    case "cas_part":
                                        casPart = ulong.Parse(diaperState.Value);
                                        break;
                                }
                            }

                            configDict[new Tuple<int, int>(wetness, messiness)] = casPart;
                        }

                        configDictList.Add(configDict);
                    }
                }
            //}

            int diaperTypeIdx = 0;

            uint prototypeId = FNV.Hash32(packageName);

            foreach (var dir in Directory.GetDirectories(textureDir).OrderBy(d => d))
            {

                var configDict = new Dictionary<Tuple<int, int>, ulong>();

                if(configDictList.Count > diaperTypeIdx)
                {
                    configDict = configDictList[diaperTypeIdx++];
                }
                else
                {
                    configDictList.Add(configDict);
                }

                var defaultSwatch = S4Studio.Color.White;

                foreach (var file in Directory.GetFiles(dir))
                {
                    
                    var diaperName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar)).Replace(' ','_');

                    if (!matcher.IsMatch(file))
                        continue;
                    var matches = matcher.Match(file);
                    int wetness = int.Parse(matches.Groups[1].Value);
                    int messiness = int.Parse(matches.Groups[2].Value);

                    

                    string name = packageName + "_" + diaperName + "_" + "w" + wetness + "m" + messiness;

                    name = name.Replace("[", "").Replace("]", "");

                    ulong partId = FNV.Hash64(name + "_caspart") | 9223372036854775808UL;

                    ulong textureId = FNV.Hash64(name) | 9223372036854775808UL;


                    var stateKey = new Tuple<int, int>(wetness, messiness);

                    CASPartResource part = null;

                    bool isNewSwatch = true;

                    if (configDict.ContainsKey(stateKey))
                    {
                        partId = configDict[stateKey];
                        isNewSwatch = false;
                        part = localSource.Find<CASPartResource>(partId);

                        if (part == null)
                        {
                            throw new Exception("Unable to find CAS part with ID " + partId);
                        }

                        if(secondaryIndex == 0)
                        {
                            secondaryIndex = part.SecondaryDisplayIndex;
                        }
                        prototypeId = part.PrototypeId;
                        defaultSwatch = part.SwatchColors[0];
                        textureId = part.DiffuseMap.Instance;
                    }
                    else
                    {
                        configDict.Add(stateKey, partId);
                    }
                   

                    var key = new ResourceKey(CASPartResource.Type, group, partId);


                    if(isNewSwatch)
                    {
                        part = new CASPartResource(key);
                    }
                    

                    if (needToImportModel)
                    {

                        part.RegionMap = tempRegionMap.Key;
                        part.Lods = gender == "f" ? femaleResource.Lods.Copy() : maleResource.Lods.Copy();


                        CASSwatch casSwatch = new CASSwatch(AppModel.Instance.GlobalFiles, package, new CASStandalone(null, GlobalSource, package, 0), part);

                        //importMes
                        ImportMesh(blender_util, options.ModelFile, tempRegionMap, localSource, AppModel.Instance.GlobalFiles, AppModel.Instance.GlobalFiles, package, part.Lods, casSwatch);

                        part.RegionMap = casSwatch.CASPart.RegionMap;

                        tempRegionMap = localSource.Find<RegionMap>(part.RegionMap.Instance);

                        needToImportModel = false;
                    }
                    else
                    {
                        RegionMap regionMap = GlobalSource.Find<RegionMap>(modelId);

                        if (regionMap == null)
                        {
                            regionMap = tempRegionMap;
                        }

                        if(regionMap == null)
                        {
                            regionMap = localSource.Find<RegionMap>(modelId);
                        }

                        if(regionMap == null)
                        {
                            throw new Exception("Cannot find model");
                        }

                        part.RegionMap = regionMap.Key;
                        part.Lods = new List<CASPartLod>();
                        part.Lods.Add(new CASPartLod());
                        part.Lods.Add(new CASPartLod());
                        part.Lods.Add(new CASPartLod());
                        part.Lods.Add(new CASPartLod());
                        byte level = 0;
                        foreach (var lod in part.Lods)
                        {
                            lod.Level = level++;
                            lod.Assets = new List<CASPartLodAsset>
                                {
                                    new CASPartLodAsset()
                                };
                            lod.Assets[0].CastShadow = 32768;
                            lod.Assets[0].SpecularLevel = 8192;
                            lod.Unused = lod.Level < 2 ? 8u : 31u;
                            lod.LodModels = new List<IResourceKey>();
                            lod.LodModels.Add(regionMap.Entries[0].Models[lod.Level]);
                        }
                    }

                    if (needToImportNormal)
                    {
                        normalId = FNV.Hash64(packageName + "_normal");
                        var normalKey = new ResourceKey(DSTImageResource.Type, group, normalId);

                        DSTImageResource normalResource = new DSTImageResource(normalKey);
                        normalResource.RawImageData = TC.ConvertData(options.NormalMapFile, normalResource.Extension, requiresAlpha: true);
                        package.Add(normalResource);

                        needToImportNormal = false;
                    }

                    if (needToImportSpecular)
                    {
                        specularId = FNV.Hash64(packageName + "_specular");
                        var specularKey = new ResourceKey(RLESImageResource.Type, group, specularId);

                        RLESImageResource specularResource = new RLESImageResource(specularKey);


                        string path = options.SpecularFile;
                        string str = options.SpecularFile.IndexOf(".mask", StringComparison.Ordinal) > 0 ? options.SpecularFile : string.Empty;
                        if (File.Exists(str))
                            options.SpecularFile = str.Replace(".mask", "");
                        else
                            str = path.Replace(Path.GetExtension(path), ".mask" + Path.GetExtension(path));
                        if (File.Exists(str))
                            specularResource.RawMaskData = TC.ConvertData(str, specularResource.MaskExtension, true);
                        byte[] rawMaskData = specularResource.RawMaskData;
                        //if ((rawMaskData == null || rawMaskData.Length == 0) && this.SwatchParent != null)
                        //    this.Image.RawMaskData = this.SwatchParent.DefaultTexture.Image.Bitmap.ExtractAlphaMask().Resize(this.Image.Bitmap.Width, this.Image.Bitmap.Height).SaveBytes();

                        specularResource.RawImageData = TC.ConvertData(options.SpecularFile, specularResource.Extension, true);

                        package.Add(specularResource);

                        needToImportSpecular = false;
                    }


                    var textureKey = new ResourceKey(RLE2ImageResource.Type, group, textureId);

                    RLE2ImageResource textureResource = new RLE2ImageResource(textureKey);
                    textureResource.RawImageData = TC.ConvertData(file, textureResource.Extension, requiresAlpha: true);
                    package.Add(textureResource);

                    part.DiffuseMap = textureKey;
                    part.SpecularMap = new ResourceKey(specularType, group, specularId);
                    part.NormalMap = new ResourceKey(normalMapType, group, normalId);
                    part.PartFlags = new CASPartFlags(visibleStates.Contains(stateKey) ? (byte)8 : (byte)0);
                    part.PrototypeId = prototypeId;
                    part.SecondaryDisplayIndex = secondaryIndex;
                    part.Tags = tags;
                    part.SlotKeys = new List<byte> { (byte)0, (byte)0 };

                    if (visibleStates.Contains(stateKey))
                    {
                        secondaryIndex += 5;
                    }

                    if (isNewSwatch)
                    {
                        part.Name = name;
                        part.AdditionalTextureSpace = partBodyType;
                        part.AgeGender = ageGender;
                        part.AuralMaterialHash = auralMaterialHash;
                        part.BodyType = partBodyType;
                        //part.Lods = new List<CASPartLod>(4);
                        
                        part.SortLayer = sortLayer;
                        part.Species = Species.Human;
                        
                        part.SwatchColors.Add(defaultSwatch);
                        part.SwatchColors.Add(getMessinessColor(messiness));
                        part.SwatchColors.Add(getWetnessColor(wetness));
                        part.SliderBrightnessMaximumValue = 0.5f;
                        part.SliderBrightnessMinimumValue = -0.5f;
                        part.SliderBrightnessStepValue = 0.05f;
                        part.SliderHueMaximumValue = 0.5f;
                        part.SliderHueMinimumValue = -0.5f;
                        part.SliderHueStepValue = 0.05f;
                        part.SliderOpacityMinimumValue = 0.2f;
                        part.SliderOpacityStepValue = 0.05f;
                        part.SliderSaturationMaximumValue = 0.5f;
                        part.SliderSaturationMinimumValue = -0.5f;
                        part.SliderSaturationStepValue = 0.05f;
                        package.Add(part);
                    }
                }
            }

            SnippetTuningResource snippet = null;

            var snippetName = "";

            if(needToParseConfig)
            {
                snippet = localSource.Find<SnippetTuningResource>(configRef.Single().Instance);
                snippetName = snippet.Name;
            }
            else
            {
                snippetName = packageName + "_config";
                snippet = new SnippetTuningResource(new ResourceKey(SnippetTuningResource.Type, group, FNV.Hash64(snippetName) | 9223372036854775808UL));

                snippet.Name = snippetName;
                snippet.TuningId = FNV.Hash64(snippetName) | 9223372036854775808UL;
                package.Add(snippet);
                
            }
            var xml = generateConfig(configDictList, partBodyType, snippet.TuningId.Value, snippetName);
            snippet.XmlText = xml;
            Console.WriteLine(xml);

            //foreach (var entry in package.FindAllByType(CASPartResource.Type))
            //{
            //    var resource = package.FetchResource<CASPartResource>(((DBPFResourcePointer)entry).Key);

            //    resource.SwatchColors[0] = S4Studio.Color.FromArgb(255, 220, 20, 60);
            //    Console.WriteLine(resource.Key.Instance);
            //}
            package.Save();
        }

        private static ClipResource LoadClip(string path)
        {
            var cr = new ClipResource();
            using (var fs = new FileStream(path, FileMode.Open))
            {
                cr.Read(fs);
            }

            return cr;
        }

        private static void RemoveUnwantedIK(ClipResource cr, params uint[] unwantedIKBones)
        {
            RemoveUnwantedIK(cr, unwantedIKBones.ToHashSet());
        }

        private static void RemoveUnwantedIK(ClipResource cr, IEnumerable<uint> unwantedIKBones)
        {
            var channelsToRemove = new List<Channel>();
            foreach (var channel in cr.Clip.Channels.Where(c => c.ChannelSubTarget != SubTargetType.Orientation && c.ChannelSubTarget != SubTargetType.Translation && c.ChannelSubTarget != SubTargetType.Scale))
            {
                if (unwantedIKBones.Contains(channel.Target))
                {
                    channelsToRemove.Add(channel);
                }
            }

            foreach (var channel in channelsToRemove)
            {
                cr.Clip.Channels.Remove(channel);
            }
            
        }


        private static void SaveClip(ClipResource clip, string targetDir)
        {
            if(!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var hash = FNV.Hash64(clip.ClipName);
            var hashHexStr = $"{hash:X}";

            var newFileName = Path.Join(targetDir, $"6B20C4F3!00000000!{hashHexStr}.{clip.ClipName}.Clip");

            using (var fs = new FileStream(newFileName, FileMode.OpenOrCreate))
            {
                clip.Write(fs);
            }

            var headerFileName = Path.Join(targetDir, $"BC4A5044!00000000!{hashHexStr}.{clip.ClipName}.ClipHeader");

            using (var fs = new FileStream(headerFileName, FileMode.OpenOrCreate))
            {
                new ClipHeaderResource(clip).Write(fs);
            }
        }
        private static void importTexture(CASSwatchTexture texture, string filePath)
        {
            texture.Import(filePath);
        }

        private static string parseClipActorAgePrefix(string clipName)
        {
            var prefix = clipName.Split('_')[0];

            if(prefix.Length == 1)
                return prefix;

            if(clipName.EndsWith('x'))
                return prefix.Substring(0, 1);

            if (clipName.EndsWith('y'))
                return prefix.Substring(prefix.Length - 1, 1);

            if (prefix.First() == prefix.Last())
                return prefix.Substring(0, 1);

            Console.WriteLine("Failed to parse clip age target");

            return prefix.Substring(0, 1); //fallback
        }

        private static string generateConfig(IList<Dictionary<Tuple<int,int>,ulong>> configDictList, BodyType bodyType, ulong id, string name)
        {
            //StringBuilder sb = new StringBuilder();

            XElement config = new XElement("I",
                new XAttribute("c", "DiaperLoadCASConfig"),
                new XAttribute("i", "snippet"),
                new XAttribute("m", "iwnbedwetting.diaper_cas_part_config.snippet"),
                new XAttribute("n", name),
                new XAttribute("s", id),
                new XElement("L", new XAttribute("n", "diaper_cc_list")));

            var configList = config.Elements("L").Single();

            foreach (var configDict in configDictList)
            {
                var diaperRoot = new XElement("U");
                diaperRoot.Add(new XElement("E", new XAttribute("n","body_type"), bodyType == BodyType.IndexFingerLeft ? "INDEX_FINGER_LEFT" : "LOWER_BODY"));
                diaperRoot.Add(new XElement("T", new XAttribute("n", "default_cas_part"), configDict[new Tuple<int, int>(0, 0)]));

                var diaperLoadRoot = new XElement("L", new XAttribute("n", "diaper_load_config"));

                for (int wetness = 0; wetness < 7; wetness++)
                {
                    for (int messiness = 0; messiness < 5; messiness++)
                    {
                        if (wetness == 0 && messiness == 0)
                            continue;
                        var key = new Tuple<int, int>(wetness, messiness);

                        if (configDict.ContainsKey(key))
                        {
                            var diaperLoad = new XElement("U");
                            if (wetness > 0)
                                diaperLoad.Add(new XElement("T", new XAttribute("n", "wetness_level"), wetness));
                            if (messiness > 0)
                                diaperLoad.Add(new XElement("T", new XAttribute("n", "mess_level"), messiness));
                            diaperLoad.Add(new XElement("T", new XAttribute("n", "cas_part"), configDict[key]));

                            diaperLoadRoot.Add(diaperLoad);
                        }
                    }
                }
                diaperRoot.Add(diaperLoadRoot);

                configList.Add(diaperRoot);
            }

            return config.ToString(SaveOptions.None);

//            sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>
//<I c=""DiaperLoadCASConfig"" i=""snippet"" m=""iwnbedwetting.diaper_cas_part_config.snippet"" n=""" + name + @""" s=""" + id + @""">
//  <L n=""diaper_cc_list"">
//    <U>");

//            switch(bodyType)
//            {
//                case BodyType.IndexFingerLeft:
//                    sb.AppendLine("      <E n=\"body_type\">INDEX_FINGER_LEFT</E>");
//                    break;
//                case BodyType.LowerBody:
//                    sb.AppendLine("      <E n=\"body_type\">LOWER_BODY</E>");
//                    break;
//                default:
//                    throw new Exception("You're doing it wrong");
//            }

//            sb.AppendLine("      <T n=\"default_cas_part\">" + configDict[new Tuple<int, int>(0, 0)] + "</T>");
//            sb.AppendLine("      <L n=\"diaper_load_config\">");

//            for(int wetness = 0; wetness < 7; wetness++)
//            {
//                for (int messiness = 0; messiness < 5; messiness++)
//                {
//                    if (wetness == 0 && messiness == 0)
//                        continue;
//                    var key = new Tuple<int, int>(wetness, messiness);

//                    if (configDict.ContainsKey(key))
//                    {
//                        sb.AppendLine("        <U>");
//                        if (wetness > 0)
//                            sb.AppendLine("          <T n=\"wetness_level\">" + wetness + "</T>");
//                        if(messiness> 0)
//                            sb.AppendLine("          <T n=\"mess_level\">" + messiness + "</T>");
//                        sb.AppendLine("          <T n=\"cas_part\">" + configDict[key] + "</T>");
//                        sb.AppendLine("        </U>");
//                    }
//                }
//            }

//            sb.AppendLine(@"      </L>
//    </U>
//  </L>
//</I>");
//            return sb.ToString();
        }

        private static string getOriginalClipName(string clipName, string origAgePrefix, IDictionary<string, string> fileLookup)
        {
            if (clipName.StartsWith("adult_"))
            {
                return clipName.Replace("adult_", "");
            }
            if (clipName.StartsWith("child_"))
            {
                return clipName.Replace("child_", "");
            }

            var origClipName = "";

            var newClipName = clipName;


            if (newClipName.StartsWith("a_") || newClipName.StartsWith("c_"))
            {
                return origAgePrefix + newClipName.Substring(1);
            }
            else if (newClipName.StartsWith("a2a_") || newClipName.StartsWith("c2c_"))
            {
                origClipName = $"a2{origAgePrefix}" + newClipName.Substring(3);

                if (!fileLookup.ContainsKey(origClipName))
                {
                    origClipName = $"{origAgePrefix}2{origAgePrefix}" + newClipName.Substring(3);
                }
                if (!fileLookup.ContainsKey(origClipName))
                {
                    origClipName = $"{origAgePrefix}2a" + newClipName.Substring(3);
                }
            }
            else if (newClipName.StartsWith("a2o_") || newClipName.StartsWith("c2o_"))
            {
                origClipName = $"{origAgePrefix}2o" + newClipName.Substring(3);
            }
            else if (newClipName.StartsWith("a2c_"))
            {
                origClipName = $"a2{origAgePrefix}" + newClipName.Substring(3);
            }
            else if (newClipName.StartsWith("c2a_"))
            {
                origClipName = $"{origAgePrefix}2a" + newClipName.Substring(3);
            }

            if (!fileLookup.ContainsKey(origClipName))
            {
                return null;
            }

            return origClipName;
        }


        private static void IncludeMesh(RegionMap RegionMap, IDBPFPackage LocalPackage, IResourceProvider LocalSource, bool IsOverride, IEnumerable<CASLODItem> Lods, IEnumerable<CASSwatch> Swatches, CASStandalone standalone)
        {
            if (RegionMap == null)
                return;
            string str = LocalPackage.Filename + "_mesh";
            RegionMap original = RegionMap;
            if (LocalSource.Find(RegionMap.Key) == null)
            {
                original = original.Copy<RegionMap>();
                if (!IsOverride)
                {
                    original.Key.Group = 2147483648U;
                    original.Key.Instance = FNV.Hash64(original.Key.Instance.ToString() + str);
                    foreach (RegionMapEntry entry in original.Entries)
                    {
                        foreach (ResourceKey model in entry.Models)
                        {
                            model.Instance = FNV.Hash64(model.Instance.ToString() + str);
                            model.Group |= 2147483648U;
                        }
                    }
                }
                Console.WriteLine("Adding original");
                LocalPackage.Add((IPackedResource)original);
            }
            Dictionary<IResourceKey, IResourceKey> dictionary = new Dictionary<IResourceKey, IResourceKey>();
            foreach (GeometryResource geometryResource in Lods.SelectMany<CASLODItem, CASLODPart>((Func<CASLODItem, IEnumerable<CASLODPart>>)(x => x.Parts.Where<CASLODPart>((Func<CASLODPart, bool>)(y => !y.IsLocal)))).GroupBy<CASLODPart, IResourceKey>((Func<CASLODPart, IResourceKey>)(x => x.GeometryKey)).Select<IGrouping<IResourceKey, CASLODPart>, GeometryResource>((Func<IGrouping<IResourceKey, CASLODPart>, GeometryResource>)(x => standalone.FindResource<GeometryResource>(x.First<CASLODPart>().GeometryKey).Copy<GeometryResource>())).Where<GeometryResource>((Func<GeometryResource, bool>)(x => x != null)).ToArray<GeometryResource>())
            {
                ResourceKey resourceKey = new ResourceKey(geometryResource.Key);
                if (!IsOverride)
                {
                    geometryResource.Key.Instance = FNV.Hash64(geometryResource.Key.Instance.ToString() + str);
                    geometryResource.Key.Group |= 2147483648U;
                    dictionary[(IResourceKey)resourceKey] = (IResourceKey)new ResourceKey(geometryResource.Key);
                }
                Console.WriteLine("Adding geometry");
                LocalPackage.Add((IPackedResource)geometryResource);
            }
            if (!IsOverride)
            {
                foreach (CASSwatch casSwatch in Swatches.OfType<CASSwatch>())
                {
                    foreach (CASPartLod lod in casSwatch.CASPart.Lods)
                    {
                        foreach (IResourceKey lodModel in lod.LodModels)
                        {
                            if (dictionary.Keys.Contains<IResourceKey>(lodModel))
                            {
                                IResourceKey resourceKey = dictionary[lodModel];
                                lodModel.Instance = resourceKey.Instance;
                                lodModel.Group = resourceKey.Group;
                            }
                        }
                    }
                    casSwatch.CASPart.RegionMap = original.Key;
                }
            }
        }

        private static void ImportMesh(BlenderUtilities blender_util, string blender_path, RegionMap RegionMap, IResourceProvider LocalSource, IResourceProvider RemoteSource, IResourceProvider GlobalSource, IDBPFPackage LocalPackage, IEnumerable<CASPartLod> lod, CASSwatch casSwatch)
        {
            string str = Path.Combine(blender_util.ScriptWorkingPath, Guid.NewGuid().ToString()) + "\\";
            try
            {
                var standalone = new CASStandalone(null, RemoteSource, LocalPackage, 0);
                var caslodItem = lod.Select(lod => new CASLODItem(RemoteSource, LocalPackage, standalone, lod));
                
                IncludeMesh(RegionMap, LocalPackage, LocalSource, false, caslodItem, new List<CASSwatch> { casSwatch }, standalone);

                var caslodItem2 = caslodItem.Single(l => l.Level == 0);


                if (!Directory.Exists(str))
                    Directory.CreateDirectory(str);
                for (int index = 0; index < caslodItem2.Parts.Count; ++index)
                {
                    GeometryResource geometry = caslodItem2.Parts[index].Geometry;
                    if (geometry != null)
                    {
                        string path1 = str;
                        DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(11, 4);
                        interpolatedStringHandler.AppendFormatted<uint>(geometry.Key.Type, "X4");
                        interpolatedStringHandler.AppendLiteral("-");
                        interpolatedStringHandler.AppendFormatted<uint>(geometry.Key.Group, "X4");
                        interpolatedStringHandler.AppendLiteral("-");
                        interpolatedStringHandler.AppendFormatted<ulong>(geometry.Key.Instance, "X8");
                        interpolatedStringHandler.AppendLiteral("-");
                        interpolatedStringHandler.AppendFormatted<int>(index, "X4");
                        interpolatedStringHandler.AppendLiteral(".simgeom");
                        string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                        using (FileStream fileStream = File.Create(Path.Combine(path1, stringAndClear)))
                            geometry.Write((Stream)fileStream);
                    }
                }
                blender_util.ImportGeom(blender_path, str, (int)casSwatch.CASPart.BodyType);
                foreach (string enumerateFile in Directory.EnumerateFiles(str, "*.simgeom"))
                {
                    string withoutExtension = Path.GetFileNameWithoutExtension(enumerateFile);
                    if (withoutExtension != null)
                    {
                        string[] strArray = withoutExtension.Split('-');
                        ResourceKey resourceKey = new ResourceKey(Convert.ToUInt32(strArray[0], 16), Convert.ToUInt32(strArray[1], 16), Convert.ToUInt64(strArray[2], 16));
                        GeometryResource geometryResource1 = new GeometryResource((IResourceKey)resourceKey);
                        using (FileStream fileStream = File.OpenRead(enumerateFile))
                            geometryResource1.Read((Stream)fileStream);
                        GeometryResource geometryResource2 = LocalSource.Find((IResourceKey)resourceKey)?.Data<GeometryResource>();
                        if (geometryResource2 != null)
                        {
                            geometryResource2.Vertices = geometryResource1.Vertices;
                            geometryResource2.Indices = geometryResource1.Indices;
                            geometryResource2.VertexFormat = geometryResource1.VertexFormat;
                            geometryResource2.Bones.Clear();
                            geometryResource2.Bones.AddRange((IEnumerable<uint>)geometryResource1.Bones);
                            geometryResource2.Stitch();
                            geometryResource2.AdjustSlotRays(casSwatch.CASPart, GlobalSource);
                            geometryResource2.ProcessVertices((byte)casSwatch.CASPart.BodyType);
                        }
                    }
                }
                CASNormalRetainer.RetainNormals(casSwatch.CASPart, GlobalSource, LocalSource);
                CASVertexStitchUtility.StitchVertices(casSwatch.CASPart, LocalSource);
            }
            finally
            {
                Directory.Delete(str, true);
            }
        }

        private static ObservableCollection<CASLODItem> initLods(CASPartResource partResource, IResourceProvider remoteSource, IDBPFPackage localPackage)
        {
            ObservableCollection<CASLODItem> lods = new ObservableCollection<CASLODItem>();
            foreach (CASPartLod lod in (IEnumerable<CASPartLod>)partResource.Lods.OrderBy<CASPartLod, byte>((Func<CASPartLod, byte>)(x => x.Level)))
                lods.Add(new CASLODItem(remoteSource, localPackage, null, lod)); ;
            return lods;
        }

        private static S4Studio.Color getWetnessColor(int wetness)
        {
            switch (wetness)
            {
                case 1:
                    return S4Studio.Color.FromArgb(255, 255, 255, 240);
                case 2:
                    return S4Studio.Color.FromArgb(255, 245, 245, 220);
                case 3:
                    return S4Studio.Color.FromArgb(255, 255, 255, 224);
                case 4:
                    return S4Studio.Color.FromArgb(255, 250, 250, 210);
                case 5:
                    return S4Studio.Color.FromArgb(255, 255, 255, 0);
                case 6:
                    return S4Studio.Color.FromArgb(255, 255, 255, 0);
                default:
                    return S4Studio.Color.FromArgb(255, 255, 255, 255);
            }
        }

        private static S4Studio.Color getMessinessColor(int messiness)
        {
            switch (messiness)
            {
                case 1:
                    return S4Studio.Color.FromArgb(255, 218, 165, 32);
                case 2:
                    return S4Studio.Color.FromArgb(255, 210, 105, 30);
                case 3:
                    return S4Studio.Color.FromArgb(255, 139, 69, 19);
                case 4:
                    return S4Studio.Color.FromArgb(255, 139, 69, 19);
                default:
                    return S4Studio.Color.FromArgb(255, 255, 255, 255);
            }
        }

        static void ExportClip(IResourceProvider resources, ClipResource clip, SimBody sim, string outputDir, ISet<string> clipList, BlenderUtilities blender_util)
        {
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            IResourceProvider GlobalSource = resources;
            //FilePickerOptions options = FilePickerOptions.Blender();
            //string sourceAssetName = clip.Clip.SourceAssetName;
            //if (!string.IsNullOrEmpty(sourceAssetName))
            //    options.DefaultFilepath = Path.GetFileNameWithoutExtension(sourceAssetName);
            
            Func<string> prefixFunc = () => { 
                if (sim.Rig == SimBody.CU.Rig)
                {
                    return "c";
                }
                if (sim.Rig == SimBody.YF.Rig || sim.Rig == SimBody.YM.Rig)
                {
                    return "a";
                }
                if(sim.Rig == SimBody.PU.Rig)
                {
                    return "p";
                }
                if(sim.Rig == SimBody.IU.Rig)
                {
                    return "i";
                }
                return "";
            };
            string targetPrefix = prefixFunc();

            string prefix = clip.ClipName.Split("_")[0];

            string fileName = clip.ClipName;

            string age = "";

            switch(prefix)
            {
                case "i":
                    age = "i";
                    fileName = prefix.Replace("i", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "a2i":
                    age = "i";
                    if (!clip.ClipName.EndsWith("_y", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Skipping due to wrong actor");
                        return;
                    }
                    fileName = prefix.Replace("i", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "i2i":
                    age = "i";
                    fileName = prefix.Replace("i", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "i2a":
                    age = "i";
                    if (!clip.ClipName.EndsWith("_x", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Skipping due to wrong actor");
                        return;
                    }
                    fileName = prefix.Replace("i", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "i2o":
                    age = "i";
                    if (!clip.ClipName.EndsWith("_x", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Skipping due to wrong actor");
                        return;
                    }
                    fileName = prefix.Replace("i", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "p":
                    age = "p";
                    fileName = prefix.Replace("p", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "a2p":
                    age = "p";
                    if (!clip.ClipName.EndsWith("_y", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Skipping due to wrong actor");
                        return;
                    }
                    fileName = prefix.Replace("p", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "p2p":
                    age = "p";
                    fileName = prefix.Replace("p", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "p2a":
                    age = "p";
                    if (!clip.ClipName.EndsWith("_x", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Skipping due to wrong actor");
                        return;
                    }
                    fileName = prefix.Replace("p", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "p2o":
                    age = "p";
                    if (!clip.ClipName.EndsWith("_x", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Skipping due to wrong actor");
                        return;
                    }
                    fileName = prefix.Replace("p", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;

                case "c":
                    age = "c";
                    fileName = prefix.Replace("c", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "a2c":
                    age = "c";
                    if (!clip.ClipName.EndsWith("_y", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Skipping due to wrong actor");
                        return;
                    }
                    fileName = prefix.Replace("c", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "c2c":
                    age = "c";
                    fileName = prefix.Replace("c", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "c2a":
                    age = "c";
                    if (!clip.ClipName.EndsWith("_x", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Skipping due to wrong actor");
                        return;
                    }
                    fileName = prefix.Replace("c", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
                case "c2o":
                    age = "c";
                    if (!clip.ClipName.EndsWith("_x", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("Skipping due to wrong actor");
                        return;
                    }
                    fileName = prefix.Replace("c", targetPrefix) + "_" + clip.ClipName.Split("_", 2)[1];
                    break;
            }

            if(age != targetPrefix)
            {
                if (clipList.Contains(fileName))
                {
                    Console.WriteLine(fileName + " already exists in game");
                    fileName = sim.Rig == SimBody.CU.Rig ? "child_" + clip.ClipName : "adult_" + clip.ClipName;
                }
            }


            fileName = fileName + ".blend";

            fileName = Path.Combine(outputDir, fileName);

            if(File.Exists(fileName))
            {
                return;
            }

            // ISSUE: explicit non-virtual call
            bool found;
            // ISSUE: explicit non-virtual call
            string str1 = Path.Combine(blender_util.ScriptWorkingPath, Guid.NewGuid().ToString());
            FileUtil.CreateDirectory(str1);
            try
            {
                string str2 = Path.Combine(str1, "CLIP") + "\\";
                string str3 = Path.Combine(str2, FileUtil.CleanFileName("output.animation"));
                Directory.CreateDirectory(str2);
                string str4 = Path.Combine(str1, Path.GetFileName(fileName));
                using (FileStream fileStream = File.Create(str3))
                    clip.Write((Stream)fileStream);
                string mannequin_folder;
                string mannequin_texture_path;
                string rig_path;
                SimBuilder.ExtractMannequin(GlobalSource, str1, sim, BodyType.None, 0, out mannequin_folder, out mannequin_texture_path, out rig_path);
                //SimBuilder.ExtractMannequin(GlobalSource, str1, sim, BodyType.None, 0, out mannequin_folder, out mannequin_texture_path, out rig_path);
                //blender_util.InstallAddon();
                //blender_util.PrepareClip(str4, mannequin_folder, mannequin_texture_path, rig_path, clip.Duration);
                blender_util.ExportClip(str4, str3, mannequin_folder, mannequin_texture_path, rig_path);
                File.Copy(str4, fileName, true);
            }
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