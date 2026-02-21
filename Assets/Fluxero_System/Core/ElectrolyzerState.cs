[System.Serializable]
public class ElectrolyzerState
{
    public float inputPowerKW;
    public float stackVoltageV;
    public float stackCurrentA;
    public float faradaicEfficiency;
    public float stackTempC;
    public float instantaneousH2RateKgHr;
    public float cumulativeH2Kg;
    public float healthIndex;
    public float sessionElapsedSeconds;
    public string timestamp;
    public bool thermalAlertActive;
    public bool lowEfficiencyAlertActive;
}