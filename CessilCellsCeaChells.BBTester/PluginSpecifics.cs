using CessilCellsCeaChells.BBTester;
using CessilCellsCeaChells.CeaChore;


#if ContentWarning
using TargetType = Player;
using TargetEnum = ShopItemCategory;
#elif LethalCompany
using TargetType = WalkieTalkie;
using TargetEnum = SettingsOptionType;
#elif RiskOfRain2
using TargetType = RoR2.Inventory;
using TargetEnum = RoR2.ItemTier;
#elif DysonSphereProgram
using TargetType = Player;
using TargetEnum = ESpaceGuideType;
#elif LastTrainWormtown
using TargetType = Player;
using TargetEnum = PlayerLook_ThirdPerson.CameraViewMode;
#elif Mechanica
using TargetType = Game.Player.PlayerMovement;
using TargetEnum = Game.Programming.MVariableType;
#endif

[assembly: RequiresField(typeof(TargetType), "BB_TestField", typeof(string))]
[assembly: RequiresProperty(typeof(TargetType), "BB_TestProp", typeof(CustomDataStruct))]
[assembly: RequiresEnumInsertion(typeof(TargetEnum), "BB_TestEnum")]
[assembly: RequiresMethod(typeof(TargetType), "BB_TestMethod", typeof(void), 
    typeof(string), typeof(int))] // void Player::BB_TestMethod(string, int)
[assembly: RequiresMethodDefaults(typeof(TargetType), "BB_TestMethod", typeof(void),
    [ typeof (bool), typeof(int) ], [ 0 ])] // void Player::BB_TestMethod(bool, int = 0)

namespace CessilCellsCeaChells.BBTester;
public struct CustomDataStruct { }