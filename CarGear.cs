using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CarGear : MonoBehaviour
{
    public TMP_Text t_speed;
    public TMP_Text t_RPM;
    public TMP_Text t_Gear;

        internal enum driveType{
        frontWheelDrive,
        rearWheelDrive,
        allWheelDrive
    }
    [SerializeField]private driveType drive;
    private WheelFrictionCurve  forwardFriction,sidewaysFriction;
    private WheelFrictionCurve  frictionCurveL,frictionCurveR,frictionCurveRF,frictionCurveLF;

    //other classes ->
    private GameManager manager;
    public inputManager IM;
    [HideInInspector]public bool playPauseSmoke = false,hasFinished;
    private carEffects CarEffects;
    [HideInInspector]public bool test; //engine sound boolean

    [Header("Variables")]
    public float maxRPM , minRPM;
    public float[] gears;
    public float[] gearChangeSpeed;
    public AnimationCurve enginePower;
    public int gearNum = 1;
    public float KPH;
    public float engineRPM;
    public bool reverse = false;
    private GameObject centerOfMass;
    public Rigidbody rigidbody;

    public float smoothTime = 0.9f;
    public float DownForceValue = 20f;

    private float radius = 4, brakPower = 0,wheelsRPM ,driftFactor, lastValue ,totalPower;

    private bool flag=false;

private WheelCollider[] wheels = new WheelCollider[4];
    private GameObject[] wheelMesh = new GameObject[4];


public Rigidbody rb;
    private Vector3 centMass;

    public float mass_x = 0;
    public float mass_y = 0;
    public float mass_z = 0;
    public GameObject carO;
    public float h_k = 2f; //horizontal steer k

        private float startPosiziton = 32f, endPosition = -211f;
        public GameObject needle;
    private float desiredPosition;
    ///////////////////////////////////////////////////////////////////////////////////////////////
    public void Start(){
        rb = GetComponent<Rigidbody>();
         o_brakesLight.material = l_materialOff;
         rb.centerOfMass = centMass;
        wheels[0] = FLWheel;
        wheels[1] = FRWheel;
        wheels[2] = BLWheel;
        wheels[3] = BRWheel;

     frictionCurveL = BLWheel.sidewaysFriction;
     frictionCurveR = BRWheel.sidewaysFriction;
     frictionCurveLF = BLWheel.forwardFriction;
     frictionCurveRF = BRWheel.forwardFriction;

    }
    public void GetInput()
    {
        m_horInput = Input.GetAxis("Horizontal");
        m_verInput = Input.GetAxis("Vertical");
    }
    private void Steer()
    {
        float m_horSteer = 0f;
        if(KPH>50){
            if(m_verInput>0)
                m_horSteer = (m_horInput / (KPH / 25)) * h_k;
            if(m_verInput==0 || m_verInput<0)
                m_horSteer = (m_horInput / (KPH / 25)) * (h_k * 1.5f);
        }
        else
            m_horSteer = m_horInput * (h_k/2);
        
                if (m_horInput > 0 ) {
				//rear tracks size is set to 1.5f       wheel base has been set to 2.55f
            wheels[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * m_horSteer;
            wheels[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * m_horSteer;
        } else if (m_horInput < 0 ) {                                                          
            wheels[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * m_horSteer;
            wheels[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * m_horSteer;
			//transform.Rotate(Vector3.up * steerHelping);

        } else {
            wheels[0].steerAngle =0;
            wheels[1].steerAngle =0;
        }
        /*m_steerAngle = maxSteerAngle * m_horInput;
        FLWheel.steerAngle = m_steerAngle;
        FRWheel.steerAngle = m_steerAngle;*/
    }
    private void Accelerate()
    {
        FLWheel.motorTorque = m_verInput * moterForce * w_throttle;
        FRWheel.motorTorque = m_verInput * moterForce * w_throttle;
    }
    private void UpdateWheelPoses()
    {
        UpdateWheelPose(FLWheel, FL);
        UpdateWheelPose(FRWheel, FR);
        UpdateWheelPose(BLWheel, BL);
        UpdateWheelPose(BRWheel, BR);
    }
        
    private void UpdateWheelPose(WheelCollider _collider, Transform _transform)
    {
        Vector3 _pos = _transform.position;
        Quaternion _quat = _transform.rotation;

        _collider.GetWorldPose(out _pos, out _quat);
        _transform.position = _pos;
        _transform.rotation = _quat;
    }

    private void FixedUpdate()
    {   

        if(((m_verInput < 0) & (wheelsRPM>0)) ||((m_verInput > 0) & (wheelsRPM<0)) ||(Input.GetKey(KeyCode.Space)) ){
            brakPower = f_brakeForce;
            o_brakesLight.material = l_materialBrakes;
        }else{
            brakPower = 0;
            o_brakesLight.material = l_materialOff;
        }
        if(reverse && m_verInput <0){
            o_ReverseLight.material = l_matetialRev;
        }else{
            o_ReverseLight.material = l_materialOff;
        }
        GetInput();
        Steer();
        //Accelerate();
        UpdateWheelPoses();
        centMass.x = carO.transform.position.x + mass_x + carO.transform.rotation.x;
        centMass.y = carO.transform.position.y + mass_y + carO.transform.rotation.y;
        centMass.z = carO.transform.position.z + mass_z + carO.transform.rotation.z;

        desiredPosition = startPosiziton - endPosition;
        float temp = engineRPM / 10000;
        needle.transform.eulerAngles = new Vector3 (0, 0, (startPosiziton - temp * desiredPosition));




        isDrift = this.GetComponent<SkidControl>().isDrifting;
        if (Input.GetKey(KeyCode.Space)){
            frictionCurveR.stiffness = 0.9f;
            frictionCurveL.stiffness = 0.9f;
            frictionCurveRF.stiffness = 1f;
            frictionCurveLF.stiffness = 1f;
            BRWheel.sidewaysFriction = frictionCurveR;
            BLWheel.sidewaysFriction = frictionCurveL;
            BRWheel.forwardFriction = frictionCurveRF;
            BLWheel.forwardFriction = frictionCurveLF;
        }else{
            if (!isDrift){
            frictionCurveR.stiffness = 1.3f;
            frictionCurveL.stiffness = 1.3f;
            frictionCurveRF.stiffness = 1.3f;
            frictionCurveLF.stiffness = 1.3f;
            BRWheel.sidewaysFriction = frictionCurveR;
            BLWheel.sidewaysFriction = frictionCurveL;
            BRWheel.forwardFriction = frictionCurveRF;
            BLWheel.forwardFriction = frictionCurveLF;
            }
        }

        
    }
    public void Update(){
        lastValue = engineRPM;
        addDownForce();
        calculateEnginePower();
        int speed = (int) KPH;
        t_speed.text = speed.ToString();
        int Rpm = (int) engineRPM;
        t_RPM.text = Rpm.ToString();
        if(reverse){
            t_Gear.text = "R";
        }
        else
            t_Gear.text = (gearNum+1).ToString();
        if(((Rpm <= 1500)&&(m_verInput==0))&&(gearNum == 0)){
            t_Gear.text = "N";
        }
    }

    private void calculateEnginePower(){

        wheelRPM();
            
            if (m_verInput != 0 ){
                rigidbody.drag = 0.005f; 
            }
            if (m_verInput == 0){
                rigidbody.drag = 0.1f;
            }
            if (gearNum == gears.Length - 1){
            if(engineRPM < maxRPM - 300)
                totalPower = 3600f * enginePower.Evaluate(engineRPM) * (m_verInput) * moterForce * powerOut;
            if(engineRPM >= maxRPM - 300)
                totalPower = 0;
            }else{
                totalPower = 3600f * enginePower.Evaluate(engineRPM) * (m_verInput) * moterForce * powerOut;
            }
        


        float velocity  = 0.0f;
        if (engineRPM >= maxRPM || flag ){
            engineRPM = Mathf.SmoothDamp(engineRPM, maxRPM - 500, ref velocity, 0.05f);

            flag = (engineRPM >= maxRPM - 450)?  true : false;
            test = (lastValue > engineRPM) ? true : false;
        }
        else { 
            engineRPM = Mathf.SmoothDamp(engineRPM,1000 + (Mathf.Abs(wheelsRPM) * 4f * (gears[gearNum])), ref velocity , smoothTime);
            test = false;
        }
        if (engineRPM >= maxRPM + 1000) engineRPM = maxRPM + 1000; // clamp at max
        moveVehicle();
    shifter();
    }

    private void addDownForce(){
        DownForceValue =20f + 20f * (KPH/50);
        rigidbody.AddForce(-transform.up * DownForceValue * rigidbody.velocity.magnitude );

    }
    private void moveVehicle(){

        if (drive == driveType.allWheelDrive){
                FLWheel.motorTorque = totalPower / 4;
                FRWheel.motorTorque = totalPower / 4;
                BLWheel.motorTorque = totalPower / 4;
                BRWheel.motorTorque = totalPower / 4;
            if(!Input.GetKey(KeyCode.Space)){
            wheels[0].brakeTorque = brakPower;
            wheels[1].brakeTorque = brakPower;
            }
            wheels[2].brakeTorque = brakPower;
            wheels[3].brakeTorque = brakPower;
        }else if(drive == driveType.rearWheelDrive){
                BLWheel.motorTorque = totalPower / 2;
                BRWheel.motorTorque = totalPower / 2;

             if(!Input.GetKey(KeyCode.Space)){
            wheels[0].brakeTorque = brakPower;
            wheels[1].brakeTorque = brakPower;
             }
            wheels[2].brakeTorque = brakPower;
            wheels[3].brakeTorque = brakPower;
            
        }
        else{
            FLWheel.motorTorque = totalPower / 2;
            FRWheel.motorTorque = totalPower / 2;

             if(!Input.GetKey(KeyCode.Space)){
            wheels[0].brakeTorque = brakPower;
            wheels[1].brakeTorque = brakPower;
             }
            wheels[2].brakeTorque = brakPower;
            wheels[3].brakeTorque = brakPower;
        }

        KPH = rigidbody.velocity.magnitude * 3.2f;


    }


    private void wheelRPM(){
        float sum = 0;
        int R = 0;
 
            sum += FRWheel.rpm;
            sum += FLWheel.rpm;
            sum += BLWheel.rpm;
            sum += BRWheel.rpm;
            R++;
        
        wheelsRPM = (R != 0) ? sum / R : 0;
 
        if(wheelsRPM < 0 && !reverse ){
            reverse = true;
        }
        else if(wheelsRPM > 0 && reverse){
            reverse = false;
        }
    }

    private bool checkGears(){
        if(KPH >= gearChangeSpeed[gearNum] ) return true;
        else return false;
    }

    private void shifter(){

        if(!isGrounded())return;
            //automatic
        if((engineRPM > maxRPM && gearNum < gears.Length-1 && !reverse && checkGears())){
            totalPower = 0;
            engineRPM-=400f;
            source.Play(0);
            gearNum ++;
            return;
        }
        if(engineRPM < minRPM && gearNum > 0){
            totalPower = 0;
            engineRPM+=400f;
            source.Play(0);
            gearNum --;

        }

    }
 
    private bool isGrounded(){
        if(wheels[0].isGrounded &&wheels[1].isGrounded &&wheels[2].isGrounded &&wheels[3].isGrounded )
            return true;
        else
            return false;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(centMass, 1);
    }

public bool isDrift;
private bool inHandbrake;
public AudioSource source;
public float powerOut;
public Material l_materialOff;
public Material l_materialBrakes;
public Material l_matetialRev;
  public Renderer o_brakesLight;
  public Renderer o_ReverseLight;
    private float w_throttle = 1;
    public float f_brakeForce;
    private float m_horInput;
    private float m_verInput;
    private float m_steerAngle;

    public WheelCollider FLWheel, FRWheel;
    public WheelCollider BLWheel, BRWheel;
    public Transform FL, FR, BL, BR;
    public float maxSteerAngle = 30;
    public float moterForce = 50;
}
