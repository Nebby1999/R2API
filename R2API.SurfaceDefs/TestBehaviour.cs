#if DEBUG
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace R2API;
public class TestBehaviour : SurfaceBehaviour
{
    [SurfaceDefAssociation]
    private static SurfaceDef GetSurfaceDef()
    {
        return Addressables.LoadAssetAsync<SurfaceDef>("cc37b32f6b38af1439d87d707d943e29").WaitForCompletion();
    }
    public override void OnEnter()
    {
        if (NetworkServer.active)
            characterBody.AddBuff(RoR2.RoR2Content.Buffs.AffixBlue);
    }

    public override void OnExit()
    {
        if(NetworkServer.active)
            characterBody.RemoveBuff(RoR2.RoR2Content.Buffs.AffixBlue);
    }
}
#endif
