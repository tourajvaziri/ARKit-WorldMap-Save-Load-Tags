using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.iOS;

/// <summary>
/// Add 3D tags in AR.
/// </summary>
public class TaggingExample : MonoBehaviour
{
    [SerializeField]
    private GameObject Tag3D;

    [SerializeField]
    private Transform Tag3DParentTransform;

    [SerializeField]
    private Button AddNewTagButton;

    [SerializeField]
    private Button LoadTagsButton;

    [SerializeField]
    private Button DeleteTagsButton;

    [SerializeField]
    private InputField TagInputField;

    [SerializeField]
    private Text BadStatusReasonText;

    [SerializeField]
    private Text BadStatusStateText;

    [SerializeField]
    private Text GoodStatusStateText;

    public WorldMapManager _worldMapManager;

    void Awake()
    {
        AddNewTagButton.interactable = false;
        TagInputField.interactable = false;
        LoadTagsButton.interactable = false;
        DeleteTagsButton.interactable = false;
        GoodStatusStateText.text = "";
        BadStatusReasonText.text = "";
        BadStatusStateText.text = "";
        UnityARSessionNativeInterface.ARFrameUpdatedEvent += CheckWorldMapStatus;
    }

    void CheckWorldMapStatus(UnityARCamera cam)
    {
        string previousBadStatusText = BadStatusStateText.text;
        string previousBadStatusReasonText = BadStatusReasonText.text;

        if (cam.worldMappingStatus == ARWorldMappingStatus.ARWorldMappingStatusMapped && cam.trackingState == ARTrackingState.ARTrackingStateNormal)
        {
            AddNewTagButton.interactable = true;
            TagInputField.interactable = true;
        }
        else
        {
            AddNewTagButton.interactable = false;
            TagInputField.interactable = false;
        }

        int persistedTag3DCounts = PlayerPrefs.GetInt("PlayerPrefsTagsCountKey", 0);

        if ((cam.worldMappingStatus == ARWorldMappingStatus.ARWorldMappingStatusMapped || cam.worldMappingStatus == ARWorldMappingStatus.ARWorldMappingStatusExtending)
        && cam.trackingState == ARTrackingState.ARTrackingStateNormal && persistedTag3DCounts > 0)
        {
            LoadTagsButton.interactable = true;
        }
        else
        {
            LoadTagsButton.interactable = false;
        }

        if (persistedTag3DCounts > 0)
        {
            DeleteTagsButton.interactable = true;
        }
        else
        {
            DeleteTagsButton.interactable = false;
        }

        switch (cam.worldMappingStatus)
        {
            case ARWorldMappingStatus.ARWorldMappingStatusMapped:
                BadStatusStateText.text = "";
                break;
            case ARWorldMappingStatus.ARWorldMappingStatusExtending:
                BadStatusStateText.text = "Extending";
                break;
            case ARWorldMappingStatus.ARWorldMappingStatusLimited:
                BadStatusStateText.text = "Limited";
                break;
            case ARWorldMappingStatus.ARWorldMappingStatusNotAvailable:
                BadStatusStateText.text = "Not Available";
                break;
        }

        switch (cam.trackingReason)
        {
            case ARTrackingStateReason.ARTrackingStateReasonNone:
                BadStatusReasonText.text = "";
                Tag3DParentTransform.gameObject.SetActive(true);
                break;
            case ARTrackingStateReason.ARTrackingStateReasonExcessiveMotion:
                BadStatusReasonText.text = "Excessive Motion";
                Tag3DParentTransform.gameObject.SetActive(false);
                break;
            case ARTrackingStateReason.ARTrackingStateReasonInitializing:
                BadStatusReasonText.text = "Initializing";
                Tag3DParentTransform.gameObject.SetActive(false);
                break;
            case ARTrackingStateReason.ARTrackingStateReasonInsufficientFeatures:
                BadStatusReasonText.text = "Insufficient Features";
                Tag3DParentTransform.gameObject.SetActive(false);
                break;
            case ARTrackingStateReason.ARTrackingStateReasonRelocalizing:
                BadStatusReasonText.text = "Loading Tags";
                Tag3DParentTransform.gameObject.SetActive(false);
                break;
        }

        if (cam.worldMappingStatus == ARWorldMappingStatus.ARWorldMappingStatusMapped && cam.trackingReason == ARTrackingStateReason.ARTrackingStateReasonNone)
        {
            if (!string.IsNullOrEmpty(previousBadStatusText) || !string.IsNullOrEmpty(previousBadStatusReasonText))
            {
                GoodStatusStateText.text = "Ready!";
                Invoke("HideGoodStatusText", 1);
            }
        }
    }

    void HideGoodStatusText()
    {
        GoodStatusStateText.text = "";
    }

    void OnDestroy()
    {
        UnityARSessionNativeInterface.ARFrameUpdatedEvent -= CheckWorldMapStatus;
    }

    void Update()
    {
        // Rotate all tags to face camera
        foreach (Transform eachTag in Tag3DParentTransform)
        {
            RotateTowardCamera(eachTag.gameObject, 180);
        }
    }

    public void InstantiateNew3DTagAndPersistAndSaveWorldMap()
    {
        AddNewTagButton.interactable = false;

        string tagText = TagInputField.text.Trim();
        GameObject new3DTag = Instantiate3DTag(tagText, Camera.main.transform.position);

        int updatedTagCounts = Tag3DParentTransform.childCount;
        PlayerPrefs.SetInt("PlayerPrefsTagsCountKey", updatedTagCounts);
        PlayerPrefs.SetString("PlayerPrefsTag_" + updatedTagCounts, tagText + "," + new3DTag.transform.position.x + "," + new3DTag.transform.position.y + "," + new3DTag.transform.position.z);
        PlayerPrefs.Save();

        _worldMapManager.Save();

        TagInputField.text = "";
    }

    public void LoadWorldMapAndInstiatePersisted3DTags()
    {
        _worldMapManager.Load();

        foreach (Transform eachTag in Tag3DParentTransform)
        {
            Destroy(eachTag.gameObject);
        }

        int persistedTag3DCounts = PlayerPrefs.GetInt("PlayerPrefsTagsCountKey", 0);
        for (int i = 1; i <= persistedTag3DCounts; i++)
        {
            string tagPersistedString = PlayerPrefs.GetString("PlayerPrefsTag_" + i);
            Debug.Log("Persisted tag transform: " + tagPersistedString);
            string[] parsedTagPersisted = tagPersistedString.Split(',');
            GameObject restoredTag = Instantiate3DTag(parsedTagPersisted[0], new Vector3(float.Parse(parsedTagPersisted[1]),
                                                                                 float.Parse(parsedTagPersisted[2]),
                                                                                 float.Parse(parsedTagPersisted[3])));
        }
    }

    public void DeleteAllPersisted3DTags()
    {
        foreach (Transform eachTag in Tag3DParentTransform)
        {
            Destroy(eachTag.gameObject);
        }

        PlayerPrefs.SetInt("PlayerPrefsTagsCountKey", 0);
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    private GameObject Instantiate3DTag(string tagText, Vector3 position)
    {
        GameObject new3DTag = Instantiate(Tag3D, Tag3DParentTransform);
        new3DTag.transform.position = position;
        new3DTag.GetComponent<TextMesh>().text = tagText;
        return new3DTag;
    }

    void RotateTowardCamera(GameObject objectToRotate, float tiltValue)
    {
        var lookAtPosition = Camera.main.transform.position - objectToRotate.transform.position;
        lookAtPosition.y = 0;
        var rotation = Quaternion.LookRotation(lookAtPosition) * Quaternion.Euler(0, tiltValue, 0);
        objectToRotate.transform.rotation = rotation;
    }

    public void OnTagInputValueChanged()
    {
        if (!string.IsNullOrEmpty(TagInputField.text.Trim()))
        {
            AddNewTagButton.interactable = true;
        }
        else
        {
            AddNewTagButton.interactable = false;
        }
    }
}
