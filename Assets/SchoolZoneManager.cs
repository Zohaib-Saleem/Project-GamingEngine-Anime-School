using UnityEngine;

public class SchoolZoneManager : MonoBehaviour
{
    public static SchoolZoneManager instance;

    public AnimeSchoolManager schoolManager;
    public Image_Fader fader;
    public GameObject Exterior;
    public GameObject Interior;
    public Transform InsidePlayerSpawn;
    public Transform InsideCameraSpawn;
    public Transform OutsidePlayerSpawn;
    public Transform OutsideCameraSpawn;
    public bool advanceTask = true;

    private void Awake()
    {
        instance = this;
    }

    public void ShowInterior()
    {
        if (Exterior != null)
            Exterior.SetActive(false);
        if (Interior != null)
            Interior.SetActive(true);
        if (SchoolNPCAmbience.instance != null)
            SchoolNPCAmbience.instance.ShowIndoor();
    }

    public void EnterSchool()
    {
        if (Exterior != null)
            Exterior.SetActive(false);
        if (Interior != null)
            Interior.SetActive(true);
        if (SchoolNPCAmbience.instance != null)
            SchoolNPCAmbience.instance.ShowIndoor();

        Teleport(InsidePlayerSpawn, InsideCameraSpawn);
    }

    public void ExitSchool()
    {
        if (Interior != null)
            Interior.SetActive(false);
        if (Exterior != null)
            Exterior.SetActive(true);
        if (SchoolNPCAmbience.instance != null)
            SchoolNPCAmbience.instance.ShowOutdoor();

        Teleport(OutsidePlayerSpawn, OutsideCameraSpawn);
    }

    void Teleport(Transform playerSpawn, Transform cameraSpawn)
    {
        if (schoolManager == null)
            return;

        if (playerSpawn != null)
        {
            schoolManager.mainCh.transform.parent = null;
            schoolManager.mainCh.transform.position = playerSpawn.position;
            schoolManager.mainCh.transform.rotation = playerSpawn.rotation;
        }

        if (cameraSpawn != null)
        {
            schoolManager.mainChCamera.transform.position = cameraSpawn.position;
            schoolManager.mainChCamera.transform.rotation = cameraSpawn.rotation;
        }

        schoolManager.mainCh.SetActive(true);
        schoolManager.mainChCamera.SetActive(true);
        schoolManager.mainChUI.SetActive(true);

        if (schoolManager.cycleCam != null)
            schoolManager.cycleCam.SetActive(false);
        if (schoolManager.CycleUi != null)
            schoolManager.CycleUi.SetActive(false);

        ThirdPersonOrbitCamBasic cam = schoolManager.mainChCamera.GetComponent<ThirdPersonOrbitCamBasic>();
        if (cam != null)
            cam.SnapBehindTarget();
    }
}
