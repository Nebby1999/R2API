using EntityStates;
using HG;
using RoR2;
using RoR2.ContentManagement;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace R2API.ScriptableObjects;

[CreateAssetMenu(fileName = "new R2APISerializableContentPack", menuName = "R2API/R2APISerializableContentPack", order = 0)]
public class R2APISerializableContentPack : ScriptableObject
{

    #region Prefabs
    [Header("Prefabs")]
    [SerializeField, Tooltip("Prefabs with a CharacterBody component")]
    private GameObject[] bodyPrefabs = Array.Empty<GameObject>();

    [SerializeField, Tooltip("Prefabs with a CharacterMaster component")]
    private GameObject[] masterPrefabs = Array.Empty<GameObject>();

    [SerializeField, Tooltip("Prefabs with a ProjectileController component")]
    private GameObject[] projectilePrefabs = Array.Empty<GameObject>();

    [SerializeField, Tooltip("Prefabs with a component that inherits from \"Run\"")]
    private GameObject[] gameModePrefabs = Array.Empty<GameObject>();

    [SerializeField, Tooltip("Prefabs with an EffectComponent component")]
    private GameObject[] effectPrefabs = Array.Empty<GameObject>();

    [SerializeField, Tooltip("Prefabs with a NetworkIdentity component that dont apply to the arrays above")]
    private GameObject[] networkedObjectPrefabs = Array.Empty<GameObject>();
    #endregion

    #region Scriptable Objects
    [Space(5)]
    [Header("Scriptable Objects")]

    [SerializeField]
    private SkillDef[] skillDefs = Array.Empty<SkillDef>();

    [SerializeField]
    private SkillFamily[] skillFamilies = Array.Empty<SkillFamily>();

    [SerializeField]
    private SceneDef[] sceneDefs = Array.Empty<SceneDef>();

    [SerializeField]
    private ItemDef[] itemDefs = Array.Empty<ItemDef>();

    [SerializeField]
    private ItemTierDef[] itemTierDefs = Array.Empty<ItemTierDef>();

    [SerializeField]
    private ItemRelationshipProvider[] itemRelationshipProviders = Array.Empty<ItemRelationshipProvider>();

    [SerializeField]
    private ItemRelationshipType[] itemRelationshipTypes = Array.Empty<ItemRelationshipType>();

    [SerializeField]
    private EquipmentDef[] equipmentDefs = Array.Empty<EquipmentDef>();

    [SerializeField]
    private BuffDef[] buffDefs = Array.Empty<BuffDef>();

    [SerializeField]
    private EliteDef[] eliteDefs = Array.Empty<EliteDef>();

    [SerializeField]
    private UnlockableDef[] unlockableDefs = Array.Empty<UnlockableDef>();

    [SerializeField]
    private SurvivorDef[] survivorDefs = Array.Empty<SurvivorDef>();

    [SerializeField]
    private ArtifactDef[] artifactDefs = Array.Empty<ArtifactDef>();

    [SerializeField]
    private SurfaceDef[] surfaceDefs = Array.Empty<SurfaceDef>();

    [SerializeField]
    private NetworkSoundEventDef[] networkSoundEventDefs = Array.Empty<NetworkSoundEventDef>();

    [SerializeField]
    private MusicTrackDef[] musicTrackDefs = Array.Empty<MusicTrackDef>();

    [SerializeField]
    private GameEndingDef[] gameEndingDefs = Array.Empty<GameEndingDef>();

    [SerializeField]
    private MiscPickupDef[] miscPickupDefs = Array.Empty<MiscPickupDef>();
    #endregion

    #region Entity States
    [Space(5)]
    [Header("EntityState Related")]


    [SerializeField]
    private EntityStateConfiguration[] entityStateConfigurations = Array.Empty<EntityStateConfiguration>();

    [SerializeField, Tooltip("Types inheriting from EntityState")]
    private SerializableEntityStateType[] entityStateTypes = Array.Empty<SerializableEntityStateType>();
    #endregion

    #region Expansion Related
    [Space(5)]
    [Header("Expansion Related")]

    [SerializeField]
    private ExpansionDef[] expansionDefs = Array.Empty<ExpansionDef>();

    [SerializeField]
    private EntitlementDef[] entitlementDefs = Array.Empty<EntitlementDef>();
    #endregion

    private ContentPack contentPack;
    #region Methods
    private ContentPack CreateContentPackPrivate()
    {
        EnsureNoFieldsAreNull();

        ContentPack cp = new ContentPack();
        cp.identifier = name;
        cp.bodyPrefabs.Add(bodyPrefabs);
        cp.masterPrefabs.Add(masterPrefabs);
        cp.projectilePrefabs.Add(projectilePrefabs);
        cp.gameModePrefabs.Add(gameModePrefabs);
        cp.effectDefs.Add(effectPrefabs.Select(go => new EffectDef(go)).ToArray());
        cp.networkedObjectPrefabs.Add(networkedObjectPrefabs);
        cp.skillDefs.Add(skillDefs);
        cp.skillFamilies.Add(skillFamilies);
        cp.sceneDefs.Add(sceneDefs);
        cp.itemDefs.Add(itemDefs);
        cp.itemTierDefs.Add(itemTierDefs);
        cp.itemRelationshipTypes.Add(itemRelationshipTypes);
        cp.equipmentDefs.Add(equipmentDefs);
        cp.buffDefs.Add(buffDefs);
        cp.eliteDefs.Add(eliteDefs);
        cp.unlockableDefs.Add(unlockableDefs);
        cp.survivorDefs.Add(survivorDefs);
        cp.artifactDefs.Add(artifactDefs);
        cp.surfaceDefs.Add(surfaceDefs);
        cp.networkSoundEventDefs.Add(networkSoundEventDefs);
        cp.musicTrackDefs.Add(musicTrackDefs);
        cp.gameEndingDefs.Add(gameEndingDefs);
        cp.miscPickupDefs.Add(miscPickupDefs);
        cp.entityStateConfigurations.Add(entityStateConfigurations);

        List<Type> list = new List<Type>();
        for (int i = 0; i < entityStateTypes.Length; i++)
        {
            Type stateType = entityStateTypes[i].stateType;
            if (stateType != null)
            {
                list.Add(stateType);
                continue;
            }
            Debug.LogWarning("SerializableContentPack \"" + base.name + "\" could not resolve type with name \"" + entityStateTypes[i].typeName + "\". The type will not be available in the content pack.");
        }
        cp.entityStateTypes.Add(list.ToArray());

        cp.expansionDefs.Add(expansionDefs);
        cp.entitlementDefs.Add(entitlementDefs);

        return cp;
    }

    /// <summary>
    /// Creates the ContentPack tied to this SerializableContentPack, or returns one if its already been created.
    /// </summary>
    /// <returns>The ContentPack tied to this SerializableContentPack</returns>
    public ContentPack GetOrCreateContentPack()
    {
        if (contentPack != null)
            return contentPack;
        else
        {
            contentPack = CreateContentPackPrivate();
            return contentPack;
        }
    }

    private void EnsureNoFieldsAreNull()
    {
        RemoveNullFields(ref bodyPrefabs);
        RemoveNullFields(ref masterPrefabs);
        RemoveNullFields(ref projectilePrefabs);
        RemoveNullFields(ref gameModePrefabs);
        RemoveNullFields(ref effectPrefabs);
        RemoveNullFields(ref networkedObjectPrefabs);

        RemoveNullFields(ref skillDefs);
        RemoveNullFields(ref skillFamilies);
        RemoveNullFields(ref sceneDefs);
        RemoveNullFields(ref itemDefs);
        RemoveNullFields(ref itemTierDefs);
        RemoveNullFields(ref itemRelationshipProviders);
        RemoveNullFields(ref itemRelationshipTypes);
        RemoveNullFields(ref equipmentDefs);
        RemoveNullFields(ref buffDefs);
        RemoveNullFields(ref eliteDefs);
        RemoveNullFields(ref unlockableDefs);
        RemoveNullFields(ref survivorDefs);
        RemoveNullFields(ref artifactDefs);
        RemoveNullFields(ref surfaceDefs);
        RemoveNullFields(ref networkSoundEventDefs);
        RemoveNullFields(ref gameEndingDefs);
        RemoveNullFields(ref musicTrackDefs);
        RemoveNullFields(ref miscPickupDefs);

        RemoveNullFields(ref entityStateConfigurations);
        RemoveNullFields(ref entityStateTypes);

        RemoveNullFields(ref expansionDefs);
        RemoveNullFields(ref entitlementDefs);

        void RemoveNullFields<T>(ref T[] array)
        {
            IEnumerable<T> nonNullValues = array.Where(obj => obj != null);
            array = nonNullValues.ToArray();
        }
    }
    #endregion
}
