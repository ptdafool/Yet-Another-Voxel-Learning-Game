using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    public GameObject mainMenuObject;
    public GameObject settingsObject;

    [Header("Main Menu UI Elements")]
    public TextMeshProUGUI seedField;

    [Header("Settings Menu UI Elements")]
    public Slider viewDstSlider;
    public TextMeshProUGUI viewDstText;
    public Slider mouseSlider;
    public TextMeshProUGUI mouseSliderTxt;
    public Toggle threadingToggle;

    Settings settings;

    private void Awake() {

        if (!File.Exists(Application.dataPath + "/Settings.cfg")) {

            Debug.Log("File 'settings.cfg' not found.  Creating new File...");
            
            settings = new Settings();
            string jsonExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
        }
        else {
            Debug.Log("Settings file found.  Loading Settings...");
            string jsonImport = File.ReadAllText(Application.dataPath + "/Settings.cfg");
            settings = JsonUtility.FromJson<Settings>(jsonImport);
        }
    }
    public void StartGame() {

        VoxelData.seed = Mathf.Abs(seedField.text.GetHashCode()) / VoxelData.WorldSizeInChunks;
        SceneManager.LoadScene("PtDaFool Gaming Voxel Game", LoadSceneMode.Single);
       
    }

    public void QuitGame() {

        Application.Quit();
    }

    public void EnterSettings() {

        viewDstSlider.value = settings.viewDistance;
        mouseSlider.value = settings.mouseSensitivity;
        threadingToggle.isOn = settings.enableThreading;
        UpdateViewDistSlider();
        UpdateMouseSlider();

        mainMenuObject.SetActive(false);
        settingsObject.SetActive(true);

    }

    public void LeaveSettings() {
        settings.viewDistance = (int)viewDstSlider.value;
        settings.mouseSensitivity = mouseSlider.value;
        settings.enableThreading = threadingToggle.isOn;

        string jsonExport = JsonUtility.ToJson(settings);
        File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);

        mainMenuObject.SetActive(true);
        settingsObject.SetActive(false);

    }

    public void UpdateViewDistSlider() {

        viewDstText.text = "View Distance: " + viewDstSlider.value;

    }

    public void UpdateMouseSlider() {

        mouseSliderTxt.text = "Mouse Sensitivity: " + mouseSlider.value.ToString("F1");

    }


}
