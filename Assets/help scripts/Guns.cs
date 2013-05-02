﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Guns : MonoBehaviour
{
    public enum weaponType { Shotgun, Machinegun, Burst, Launcher }; // use burst for single shot weapons like pistols / sniper rifles
    public weaponType typeOfGun;

    

    // basic weapon variables all guns have in common
    // Objects, effects and tracers
    public Renderer muzzleFlash = null;     // the muzzle flash for this weapon
    public Light lightFlash = null;         // the light flash for this weapon
    public GameObject tracer = null;        // tracer...might get rid of this... might not depending on how multiplayer testing goes
    public Transform muzzlePoint = null;    // the muzzle point of this weapon
    public Transform ejectPoint = null;     // the ejection point
    public Transform mountPoint = null;     // the mount point.... more for weapon swapping then anything
    public GameObject bullet = null;        // the weapons bullet object
    public GameObject grenade = null;
    public GameObject rocket = null;
    public Rigidbody shell = null;          // the weapons empty shell object
    public GameObject gunOwner = null;      // the gun owner
    public GameObject mainCamera = null;   // the player's main camera
    public GameObject weaponCamera = null;  // this weapon's camera

    //Machinegun Vars
    private bool isFiring = false;   // is the machine gun firing?  used for decreasing accuracy while sustaining fire

    //Shotgun Specific Vars
    public int pelletsPerShot = 10;  // number of pellets per round fired for the shotgun

    //Burst Specific Vars
    public int roundsPerBurst = 3;    // number of rounds per burst fire
    public float lagBetweenShots = 0.5f; // time between each shot in a burst

    //Launcher Specific Vars
    

    // basic stats
    public int range = 300;             // range for raycast bullets... bulletType = Ray
    public float damage = 20.0f;        // bullet damage
    public float maxPenetration = 3.0f; // how many impacts the bullet can survive
    public float fireRate = 0.5f;       // how fast the gun shoots... time between shots can be fired
    public int impactForce = 50;        // how much force applied to a rigid body
    public float bulletSpeed = 200.0f;  // how fast are your bullets

    public int bulletsPerClip = 50;     // number of bullets in each clip
    public int numberOfClips = 5;       // number of clips you start with
    public int maxNumberOfClips = 10;   // maximum number of clips you can hold
    private int bulletsLeft;            // bullets in the gun-- current clip
        
    public float baseSpread = 1.0f;      // how accurate the weapon starts out... smaller the number the more accurate
    public float maxSpread = 4.0f;       // maximum inaccuracy for the weapon
    public float spreadPerSecond = 0.2f; // if trigger held down, increase the spread of bullets
    public float spread = 0.0f;          // current spread of the gun
    public float decreaseSpreadPerSec = 0.5f;  // amount of accuracy regained per frame when the gun isn't being fired 
    
    public float reloadTime = 1.0f;      // time it takes to reload the weapon
    private bool isReloading = false;    // am I in the process of reloading
    // used for tracer rendering
    public int shotsFired = 0;           // shots fired since last tracer round
    public int roundsPerTracer = 1;      // number of rounds per tracer

    private int m_LastFrameShot = -1;    // last frame a shot was fired
    private float nextFireTime = 0.0f;   // able to fire again on this frame

    private float[] bulletInfo = new float[6];  // all of the info sent to a fired bullet

    public bool isRocketLauncher = false;

    //Network Parts ...yeah
    bool localPlayer = true; //set to false   // Am I a local player... or networked
    //string localPlayerName = "";          // what's my name
    //Transform myTrans;                    // my transform


    // Setting up variables as soon as a level starts
    void Start()
    {
        //myTrans = transform;
        bulletsLeft = bulletsPerClip; // load gun on startup
        //localPlayerName = PlayerPrefs.GetString("playerName");  // get the name of the player         
    }
    // check whats the player is doing every frame
    bool Update()
    {
        if (!localPlayer)
        {
            return false;  // if not the local player.... exit function
        }
       
        // Did the user press fire.... and what kind of weapon are they using ?  ===============
        switch (typeOfGun)
        {
            case weaponType.Shotgun:
                if (Input.GetButtonDown("Fire1"))
                {
                    //Debug.Log("Shotgun Fire Called");
                    ShotGun_Fire();  // fire shotgun
                }
                break;
            case weaponType.Machinegun:
                if (Input.GetButton("Fire1"))
                {                    
                    MachineGun_Fire();   // fire machine gun                 
                }
                break;
            case weaponType.Burst:
                if (Input.GetButtonDown("Fire1"))
                {
                   StartCoroutine("Burst_Fire"); // fire off a burst of rounds                   
                }
                break;

            case weaponType.Launcher:
                if (Input.GetButtonDown("Fire1"))
                {
                    Launcher_Fire();
                }
                break;
        }//=========================================================================================

        if (Input.GetButton("Fire2"))
        {
            if (weaponCamera)
            {
                weaponCamera.camera.enabled = true;
                mainCamera.camera.enabled = false;
            }
        }
        else
        {
            weaponCamera.camera.enabled = false;
            mainCamera.camera.enabled = true;
        }


        //used to decrease weapon accuracy as long as the trigger remains down =====================
        if (Input.GetButtonDown("Fire1"))
        {
            isFiring = true; // fire is down, gun is firing
        }
        if (Input.GetButtonUp("Fire1"))
        {
            isFiring = false; // if fire is up... gun is not firing
        }
        if (isFiring) // if the gun is firing
        {
            spread += spreadPerSecond; // gun is less accurate with the trigger held down
        }
        else
        {
            spread -= decreaseSpreadPerSec; // gun regains accuracy when trigger is released
        }
        //===========================================================================================
        return true;
    }
    // update weapon flashes after checking user inout in update function
    void LateUpdate()
    {
        if (muzzleFlash || lightFlash)  // need to have a muzzle or light flash in order to enable or disable them
        {
            // We shot this frame, enable the muzzle flash
            if (m_LastFrameShot == Time.frameCount)
            {
                muzzleFlash.transform.localRotation = Quaternion.AngleAxis(Random.value * 57.3f, Vector3.forward);
                muzzleFlash.enabled = true;// enable the muzzle and light flashes
                lightFlash.enabled = true;
            }
            else
            {
                muzzleFlash.enabled = false; // disable the light and muzzle flashes
                lightFlash.enabled = false;
            }
        }

        if (spread >= maxSpread)
        {
            spread = maxSpread;  //if current spread is greater then max... set to max
        }
        else
        {
            if (spread <= baseSpread)
            {
                spread = baseSpread; //if current spread is less then base, set to base
            }
        }
    }
    // fire the machine gun
    void MachineGun_Fire()
    {
        if (bulletsLeft <= 0)
        {
            StartCoroutine("reload");
            return;
        }
        // If there is more than one bullet between the last and this frame
        // Reset the nextFireTime
        if (Time.time - fireRate > nextFireTime)
            nextFireTime = Time.time - Time.deltaTime;

        // Keep firing until we used up the fire time
        while (nextFireTime < Time.time)
        {
            StartCoroutine("FireOneShot");
            shotsFired++;
            bulletsLeft--;
            nextFireTime += fireRate;
            EjectShell();
        }
        
    }
    // fire the burst rifle
    IEnumerator Burst_Fire()
    {
        int shotCounter = 0;

        if (bulletsLeft <= 0)
        {
            StartCoroutine("reload");
            yield break;//return;
        }

        // If there is more than one bullet between the last and this frame
        // Reset the nextFireTime
        if (Time.time - fireRate > nextFireTime)
            nextFireTime = Time.time - Time.deltaTime;

        // Keep firing until we used up the fire time
        while (nextFireTime < Time.time)
        {
            while (shotCounter < roundsPerBurst)
            {
                Debug.Log(" shotCounter = " + shotCounter + ", roundsPerBurst = "+roundsPerBurst);
                StartCoroutine("FireOneShot");                
                //Debug.Log("FireOneShot Called in Fire function.");
                shotCounter++;
                shotsFired++;
                bulletsLeft--; // subtract a bullet 
                EjectShell();
                yield return new WaitForSeconds(lagBetweenShots);                
            }

            nextFireTime += fireRate;
        }
    }
    // fire the shotgun
    void ShotGun_Fire()
    {
        int pelletCounter = 0;  // counter used for pellets per round

        if (bulletsLeft == 0)
        {
            StartCoroutine("reload"); // if out of ammo, reload
            return;
        }

        // If there is more than one bullet between the last and this frame
        // Reset the nextFireTime
        if (Time.time - fireRate > nextFireTime)
            nextFireTime = Time.time - Time.deltaTime;

        // Keep firing until we used up the fire time
        while (nextFireTime < Time.time)
        {
            do
            {
                StartCoroutine("FireOneShot"); // fire 1 round
                pelletCounter++; // add another pellet
                shotsFired++; // another shot was fired                
            } while (pelletCounter < pelletsPerShot); // if number of pellets fired is less then pellets per round... fire more pellets
            EjectShell(); // eject 1 shell 
            nextFireTime += fireRate;  // can fire another shot in "firerate" number of frames
            bulletsLeft--; // subtract a bullet
        }
    }
    // fire your launcher
    void Launcher_Fire()
    {
        if (bulletsLeft > 0)
        {
            Vector3 position = muzzlePoint.position; // position to spawn rocket / grenade is at the muzzle point of the gun

            bulletInfo[0] = damage;
            bulletInfo[1] = impactForce;
            bulletInfo[2] = maxPenetration;
            bulletInfo[3] = maxSpread;
            bulletInfo[4] = spread;
            bulletInfo[5] = bulletSpeed;

            if (isRocketLauncher)
            {
                GameObject newRocket = Instantiate(rocket, position, transform.parent.rotation) as GameObject;
                newRocket.SendMessageUpwards("SetUp", bulletInfo);
            }
            else
            {
                GameObject newNoobTube = Instantiate(grenade, position, transform.parent.rotation) as GameObject;
                newNoobTube.SendMessageUpwards("SetUp", bulletInfo);
            }
            bulletsLeft--;
        }

        if (bulletsLeft == 0)
        {
            StartCoroutine("reload");
        }
    }
    // Create and fire a bullet
    IEnumerator FireOneShot()
    {
        Vector3 position = muzzlePoint.position; // position to spawn bullet is at the muzzle point of the gun       

        // set the gun's info into an array to send to the bullet
        bulletInfo[0] = damage;
        bulletInfo[1] = impactForce;
        bulletInfo[2] = maxPenetration;
        bulletInfo[3] = maxSpread;
        bulletInfo[4] = spread;
        bulletInfo[5] = bulletSpeed;

        //bullet info is set up in start function
        GameObject newBullet = Instantiate(bullet, position, transform.parent.rotation) as GameObject; // create a bullet
        newBullet.SendMessageUpwards("SetUp", bulletInfo); // send the gun's info to the bullet
        newBullet.GetComponent<Bullets>().Owner = gunOwner; // owner of the bullet is this gun's owner object

        if (!(typeOfGun == weaponType.Launcher))
        {
            if (shotsFired >= roundsPerTracer) // tracer round every so many rounds fired... is there a tracer this round fired?
            {
                newBullet.renderer.enabled = true; // turn on tracer effect
                shotsFired = 0;                    // reset tracer counter
            }
            else
            {
                newBullet.renderer.enabled = false; // turn off tracer effect
            }

            if (audio)
            {
                audio.Play();  // if there is a gun shot sound....play it
            }
        }       

        if ((bulletsLeft == 0))
        {
            StartCoroutine("reload");  // if out of bullets.... reload
            yield break;
        }
        
        // Register that we shot this frame,
        // so that the LateUpdate function enabled the muzzleflash renderer for one frame
        m_LastFrameShot = Time.frameCount;
    }
    // create and "fire" an empty shell
    void EjectShell()
    {
        Vector3 position = ejectPoint.position; // ejectile spawn point at gun's ejection point
        
        if (shell)
        {
            Rigidbody newShell = Instantiate(shell, position, transform.parent.rotation) as Rigidbody; // create empty shell
            //give ejectile a slightly random ejection velocity and direction
            newShell.velocity = transform.TransformDirection(Random.Range(-2, 2) - 3.0f, Random.Range(-1, 2) + 3.0f, -Random.Range(-2, 2) + 1.0f);
        }
    }
    // reload your weapon
    IEnumerator reload()
    {
        if (isReloading)
        {
            yield break; // if already reloading... exit and wait till reload is finished
        }

        if (numberOfClips > 0)
        {
            isReloading = true; // we are now reloading
            numberOfClips--; // take away a clip
            yield return new WaitForSeconds(reloadTime); // wait for set reload time
            bulletsLeft = bulletsPerClip; // fill up the gun
        }

        isReloading = false; // done reloading
    }
}