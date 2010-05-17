//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Camera driver for SXCamera
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM Camera interface version: 1.0
// Author:		(XXX) Your N. Here <your@email.here>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// dd-mmm-yyyy	XXX	1.0.0	Initial edit, from ASCOM Camera Driver template
// --------------------------------------------------------------------------------
//
using System;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

using ASCOM;
using ASCOM.Helper;
using ASCOM.Helper2;
using ASCOM.Interface;

using Logging;

namespace ASCOM.SXGeneric
{
    //
    // Your driver's ID is ASCOM.SXCamera.Camera
    //
    // The Guid attribute sets the CLSID for ASCOM.SXCamera.Camera
    // The ClassInterface/None addribute prevents an empty interface called
    // _Camera from being created and used as the [default] interface
    //
    [ClassInterface(ClassInterfaceType.None)]
    abstract public class Camera :
        ASCOM.SXCamera.ReferenceCountedObjectBase, 
        ICamera
    {
        private bool bConnected;
        protected sx.Camera sxCamera;
        private DateTime exposureStart;
        private TimeSpan desiredExposureLength;
        private TimeSpan actualExposureLength;
        private delegate void CaptureDelegate(double Duration, bool Light);
        private bool bImageValid;
        private short binX, binY;
        private volatile Object oCameraStateLock;
        private volatile CameraStates state;
        private volatile bool bAbortRequested;
        private volatile bool bStopRequested;
        private volatile Object oGuideStateLock;
        private volatile bool bGuiding;
        private static UInt16 cameraId;

        #region Camera Constructor
         //
        // Constructor - Must be public for COM registration!
        //
        protected Camera(UInt16 whichCamera)
        {
            //Thread.Sleep(15000);
            Log.Write("In Camera Constructor for camera " + whichCamera +" \n");

            oCameraStateLock = new Object();
            oGuideStateLock = new Object();
            cameraId = whichCamera;                     
        }
        #endregion

        //
        // PUBLIC COM INTERFACE ICamera IMPLEMENTATION
        //

        #region ICamera Members


        /// <summary>
        /// Aborts the current exposure, if any, and returns the camera to Idle state.
        /// Must throw exception if camera is not idle and abort is
        ///  unsuccessful (or not possible, e.g. during download).
        /// Must throw exception if hardware or communications error
        ///  occurs.
        /// Must NOT throw an exception if the camera is already idle.
        /// </summary>
        public void AbortExposure()
        {
            Log.Write("AbortExposure\n");
            lock (oCameraStateLock)
            {
                switch (state)
                {
                    case CameraStates.cameraIdle:
                        // nothing to do
                        break;
                    case CameraStates.cameraExposing:
                        bAbortRequested = true;
                        break;
                    default:
                        throw new System.Exception("Abort not possible.");
                }
            }
        }

        /// <summary>
        /// Sets the binning factor for the X axis.  Also returns the current value.  Should
        /// default to 1 when the camera link is established.  Note:  driver does not check
        /// for compatible subframe values when this value is set; rather they are checked
        /// upon StartExposure.
        /// </summary>
        /// <value>BinX sets/gets the X binning value</value>
        /// <exception>Must throw an exception for illegal binning values</exception>
        public short BinX
        {
            get
            {
                Log.Write("BinX get\n");
                return sxCamera.xBin;
            }
            set
            {
                Log.Write("BinX set\n");
                sxCamera.xBin = (byte)value;
            }
        }

        /// <summary>
        /// Sets the binning factor for the Y axis  Also returns the current value.  Should
        /// default to 1 when the camera link is established.  Note:  driver does not check
        /// for compatible subframe values when this value is set; rather they are checked
        /// upon StartExposure.
        /// </summary>
        /// <exception>Must throw an exception for illegal binning values</exception>
        public short BinY
        {
            get
            {
                Log.Write("BinY get\n");
                return sxCamera.yBin;
            }
            set
            {
                Log.Write("BinY set\n");
                sxCamera.yBin = (byte)value;
            }
        }

        /// <summary>
        /// Returns the current CCD temperature in degrees Celsius. Only valid if
        /// CanControlTemperature is True.
        /// </summary>
        /// <exception>Must throw exception if data unavailable.</exception>
        public double CCDTemperature
        {
            get
            {
                Log.Write("CCDTemperature\n"); 
                throw new System.Exception("CCDTemperature is not supported");
            }
        }

        /// <summary>
        /// Returns one of the following status information:
        /// <list type="bullet">
        ///  <listheader>
        ///   <description>Value  State          Meaning</description>
        ///  </listheader>
        ///  <item>
        ///   <description>0      CameraIdle      At idle state, available to start exposure</description>
        ///  </item>
        ///  <item>
        ///   <description>1      CameraWaiting   Exposure started but waiting (for shutter, trigger,
        ///                        filter wheel, etc.)</description>
        ///  </item>
        ///  <item>
        ///   <description>2      CameraExposing  Exposure currently in progress</description>
        ///  </item>
        ///  <item>
        ///   <description>3      CameraReading   CCD array is being read out (digitized)</description>
        ///  </item>
        ///  <item>
        ///   <description>4      CameraDownload  Downloading data to PC</description>
        ///  </item>
        ///  <item>
        ///   <description>5      CameraError     Camera error condition serious enough to prevent
        ///                        further operations (link fail, etc.).</description>
        ///  </item>
        /// </list>
        /// </summary>
        /// <exception cref="System.Exception">Must return an exception if the camera status is unavailable.</exception>
        public CameraStates CameraState
        {
            get 
            { 
                Log.Write("CameraState()\n"); 
                lock (oCameraStateLock)
                {
                    return state;
                }
            }
        }

        /// <summary>
        /// Returns the width of the CCD camera chip in unbinned pixels.
        /// </summary>
        /// <exception cref="System.Exception">Must throw exception if the value is not known</exception>
        public int CameraXSize
        {
            get
            {
                Log.Write("CameraXSize()\n");
                return sxCamera.ccdWidth;
            }
        }

        /// <summary>
        /// Returns the height of the CCD camera chip in unbinned pixels.
        /// </summary>
        /// <exception cref="System.Exception">Must throw exception if the value is not known</exception>
        public int CameraYSize
        {
            get
            {
                Log.Write("CameraYSize()\n");
                return sxCamera.ccdHeight;
            }
        }

        /// <summary>
        /// Returns True if the camera can abort exposures; False if not.
        /// </summary>
        public bool CanAbortExposure
        {
            get
            {
                Log.Write("CanAbortExposure()\n");
                return true;
            }
        }

        /// <summary>
        /// If True, the camera can have different binning on the X and Y axes, as
        /// determined by BinX and BinY. If False, the binning must be equal on the X and Y
        /// axes.
        /// </summary>
        /// <exception cref="System.Exception">Must throw exception if the value is not known (n.b. normally only
        ///            occurs if no link established and camera must be queried)</exception>
        public bool CanAsymmetricBin
        {
            get
            {
                Log.Write("CanAssymetricBin()\n");
                return true;
            }
        }

        /// <summary>
        /// If True, the camera's cooler power setting can be read.
        /// </summary>
        public bool CanGetCoolerPower
        {
            get
            {
                Log.Write("CanGetCoolerPower()\n"); 
                return false;
            }
        }

        /// <summary>
        /// Returns True if the camera can send autoguider pulses to the telescope mount; 
        public abstract bool CanPulseGuide
        {
            get;
        }

        /// False if not.  (Note: this does not provide any indication of whether the
        /// autoguider cable is actually connected.)
        /// </summary>


        /// <summary>
        /// If True, the camera's cooler setpoint can be adjusted. If False, the camera
        /// either uses open-loop cooling or does not have the ability to adjust temperature
        /// from software, and setting the TemperatureSetpoint property has no effect.
        /// </summary>
        public bool CanSetCCDTemperature
        {
            get
            {
                Log.Write("CanSetCCDTemperature()\n");
                return false;
            }
        }

        /// <summary>
        /// Some cameras support StopExposure, which allows the exposure to be terminated
        /// before the exposure timer completes, but will still read out the image.  Returns
        /// True if StopExposure is available, False if not.
        /// </summary>
        /// <exception cref=" System.Exception">not supported</exception>
        /// <exception cref=" System.Exception">an error condition such as link failure is present</exception>
        public bool CanStopExposure
        {
            get
            {
                Log.Write("CanStopExposure()\n");
                return true;
            }
        }

        /// <summary>
        /// Controls the link between the driver and the camera. Set True to enable the
        /// link. Set False to disable the link (this does not switch off the cooler).
        /// You can also read the property to check whether it is connected.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if unsuccessful.</exception>
        public bool Connected
        {
            get
            {
                Log.Write("Connected:  get - returning " + bConnected +"\n");
                return  bConnected;
            }
            set
            {
                Log.Write("Connected: set to " + value + " requested. Current value is " + bConnected +"\n");
                
                if (value)
                {
                    if (DateTime.Now.CompareTo(DateTime.Parse("07/31/2010")) > 0)
                    {
                        throw new System.Exception("Alpha release expired");
                    }

                    if (bConnected)
                    {
                        throw new System.Exception("Attempt to connect when already connected");
                    }

                    sxCamera = new sx.Camera(SXCamera.SharedResources.controller, cameraId);
                    NumX = sxCamera.ccdWidth;
                    NumY = sxCamera.ccdHeight;
                    binX = binY = 1;
                    state = CameraStates.cameraIdle;
                    bImageValid = false;
                    bAbortRequested = false;
                    bStopRequested = false;
                    bGuiding = false;
                }
                else
                {
                    sxCamera = null;
                    
                }

                bConnected = value;
            }
        }

        /// <summary>
        /// Turns on and off the camera cooler, and returns the current on/off state.
        /// Warning: turning the cooler off when the cooler is operating at high delta-T
        /// (typically >20C below ambient) may result in thermal shock.  Repeated thermal
        /// shock may lead to damage to the sensor or cooler stack.  Please consult the
        /// documentation supplied with the camera for further information.
        /// </summary>
        /// <exception cref=" System.Exception">not supported</exception>
        /// <exception cref=" System.Exception">an error condition such as link failure is present</exception>
        public bool CoolerOn
        {
            get
            {
                Log.Write("CoolerOn() get\n");
                return true;
            }
            set
            {
                Log.Write("CoolerOn() set\n");
                throw new System.Exception("CoolerOn is not supported");
            }
        }

        /// <summary>
        /// Returns the present cooler power level, in percent.  Returns zero if CoolerOn is
        /// False.
        /// </summary>
        /// <exception cref=" System.Exception">not supported</exception>
        /// <exception cref=" System.Exception">an error condition such as link failure is present</exception>
        public double CoolerPower
        {
            get 
            {
                Log.Write("CoolerPower()\n");
                return 100;
            }
        }

        /// <summary>
        /// Returns a description of the camera model, such as manufacturer and model
        /// number. Any ASCII characters may be used. The string shall not exceed 68
        /// characters (for compatibility with FITS headers).
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if description unavailable</exception>
        public virtual string Description
        {
            get 
            {
                Log.Write("Generic Description()\n");

                return sxCamera.description;
            }
        }

        /// <summary>
        /// Returns the gain of the camera in photoelectrons per A/D unit. (Some cameras have
        /// multiple gain modes; these should be selected via the SetupDialog and thus are
        /// static during a session.)
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if data unavailable.</exception>
        public double ElectronsPerADU
        {
            get 
            {
                Log.Write("ElectronsPerADU()\n");
                return sxCamera.electronsPerADU;
            }
        }

        /// <summary>
        /// Reports the full well capacity of the camera in electrons, at the current camera
        /// settings (binning, SetupDialog settings, etc.)
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if data unavailable.</exception>
        public double FullWellCapacity
        {
            get 
            {
                Log.Write("FullWellCapacity()\n");
                return MaxADU * ElectronsPerADU /(BinX * BinY);
            }
        }

        /// <summary>
        /// If True, the camera has a mechanical shutter. If False, the camera does not have
        /// a shutter.  If there is no shutter, the StartExposure command will ignore the
        /// Light parameter.
        /// </summary>
        public bool HasShutter
        {
            get 
            {
                Log.Write("HasShutter()\n"); 
                return false;
            }
        }

        /// <summary>
        /// Returns the current heat sink temperature (called "ambient temperature" by some
        /// manufacturers) in degrees Celsius. Only valid if CanControlTemperature is True.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if data unavailable.</exception>
        public double HeatSinkTemperature
        {
            get
            { 
                Log.Write("HeatSinkTemperature()\n");
                throw new System.Exception("HeatSinkTemperature is not valid if CanControlTemperature == false");
            }
        }

        /// <summary>
        /// Returns a safearray of int of size NumX * NumY containing the pixel values from
        /// the last exposure. The application must inspect the Safearray parameters to
        /// determine the dimensions. Note: if NumX or NumY is changed after a call to
        /// StartExposure it will have no effect on the size of this array. This is the
        /// preferred method for programs (not scripts) to download images since it requires
        /// much less memory.
        ///
        /// For color or multispectral cameras, will produce an array of NumX * NumY *
        /// NumPlanes.  If the application cannot handle multispectral images, it should use
        /// just the first plane.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if data unavailable.</exception>
        public object ImageArray
        {
            get 
            {
                Log.Write("ImageArray()\n"); 
                if (!bImageValid)
                {
                    throw new System.Exception("The image is not valid.");
                }
                return sxCamera.ImageArray;
            }
        }

        /// <summary>
        /// Returns a safearray of Variant of size NumX * NumY containing the pixel values
        /// from the last exposure. The application must inspect the Safearray parameters to
        /// determine the dimensions. Note: if NumX or NumY is changed after a call to
        /// StartExposure it will have no effect on the size of this array. This property
        /// should only be used from scripts due to the extremely high memory utilization on
        /// large image arrays (26 bytes per pixel). Pixels values should be in Short, int,
        /// or Double format.
        ///
        /// For color or multispectral cameras, will produce an array of NumX * NumY *
        /// NumPlanes.  If the application cannot handle multispectral images, it should use
        /// just the first plane.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if data unavailable.</exception>
        public object ImageArrayVariant
        {
            get
            { 
                Log.Write("ImageArrayVariant()\n"); 
                throw new System.Exception("The method or operation is not implemented."); 
            }
        }

        /// <summary>
        /// If True, there is an image from the camera available. If False, no image
        /// is available and attempts to use the ImageArray method will produce an
        /// exception.
        /// </summary>
        /// <exception cref=" System.Exception">hardware or communications link error has occurred.</exception>
        public bool ImageReady
        {
            get 
            {
                return bImageValid;
            }
        }

        /// <summary>
        /// If True, pulse guiding is in progress. Required if the PulseGuide() method
        /// (which is non-blocking) is implemented. See the PulseGuide() method.
        /// </summary>
        /// <exception cref=" System.Exception">hardware or communications link error has occurred.</exception>
        public bool IsPulseGuiding
        {
            get 
            {
                Log.Write("IsPulseGuiding()\n");
                lock (oGuideStateLock)
                {
                    return bGuiding;
                }
            }
        }

        /// <summary>
        /// Reports the last error condition reported by the camera hardware or communications
        /// link.  The string may contain a text message or simply an error code.  The error
        /// value is cleared the next time any method is called.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if no error condition.</exception>
        public string LastError
        {
            get 
            {
                Log.Write("LastError()\n"); 
                throw new System.Exception("The method or operation is not implemented.");
            }
        }

        /// <summary>
        /// Reports the actual exposure duration in seconds (i.e. shutter open time).  This
        /// may differ from the exposure time requested due to shutter latency, camera timing
        /// precision, etc.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if not supported or no exposure has been taken</exception>
        public double LastExposureDuration
        {
            get 
            {
                Log.Write("LastExposureDuration()\n"); 
                return actualExposureLength.TotalSeconds;
            }
        }

        /// <summary>
        /// Reports the actual exposure start in the FITS-standard
        /// CCYY-MM-DDThh:mm:ss[.sss...] format.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if not supported or no exposure has been taken</exception>
        public string LastExposureStartTime
        {
            get 
            {
                Log.Write("LastExposureStartTime()\n"); 
                return exposureStart.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            }
        }

        /// <summary>
        /// Reports the maximum ADU value the camera can produce.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if data unavailable.</exception>
        public int MaxADU
        {
            get 
            {
                Log.Write("MaxADU()\n");
                return ( 1 << sxCamera.bitsPerPixel) - 1;
            }
        }

        /// <summary>
        /// If AsymmetricBinning = False, returns the maximum allowed binning factor. If
        /// AsymmetricBinning = True, returns the maximum allowed binning factor for the X axis.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if data unavailable.</exception>
        public short MaxBinX
        {
            get 
            {
                Log.Write("MaxBinX()\n"); 
                return sxCamera.xBinMax;
            }
        }

        /// <summary>
        /// If AsymmetricBinning = False, equals MaxBinX. If AsymmetricBinning = True,
        /// returns the maximum allowed binning factor for the Y axis.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if data unavailable.</exception>
        public short MaxBinY
        {
            get 
            {
                Log.Write("MaxBinY()\n"); 
                return sxCamera.yBinMax;
            }
        }

        /// <summary>
        /// Sets the subframe width. Also returns the current value.  If binning is active,
        /// value is in binned pixels.  No error check is performed when the value is set.
        /// Should default to CameraXSize.
        /// </summary>
        public int NumX
        {
            get;
            set;
        }

        /// <summary>
        /// Sets the subframe height. Also returns the current value.  If binning is active,
        /// value is in binned pixels.  No error check is performed when the value is set.
        /// Should default to CameraYSize.
        /// </summary>
        public int NumY
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the width of the CCD chip pixels in microns, as provided by the camera
        /// driver.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if data unavailable.</exception>
        public double PixelSizeX
        {
            get 
            {
                Log.Write("PixelSizeX()\n"); 
                return sxCamera.pixelWidth;
            }
        }

        /// <summary>
        /// Returns the height of the CCD chip pixels in microns, as provided by the camera
        /// driver.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if data unavailable.</exception>
        public double PixelSizeY
        {
            get 
            {
                Log.Write("PixelSizeY()\n"); 
                return sxCamera.pixelHeight;
            }
        }

        /// <summary>
        /// This method returns only after the move has completed.
        ///
        /// symbolic Constants
        /// The (symbolic) values for GuideDirections are:
        /// Constant     Value      Description
        /// --------     -----      -----------
        /// guideNorth     0        North (+ declination/elevation)
        /// guideSouth     1        South (- declination/elevation)
        /// guideEast      2        East (+ right ascension/azimuth)
        /// guideWest      3        West (+ right ascension/azimuth)
        ///
        /// Note: directions are nominal and may depend on exact mount wiring.  guideNorth
        /// must be opposite guideSouth, and guideEast must be opposite guideWest.
        /// </summary>
        /// <param name="Direction">Direction of guide command</param>
        /// <param name="Duration">Duration of guide in milliseconds</param>
        /// <exception cref=" System.Exception">PulseGuide command is unsupported</exception>
        /// <exception cref=" System.Exception">PulseGuide command is unsuccessful</exception>
        public void PulseGuide(GuideDirections Direction, int Duration)
        {
            Log.Write("PulseGuide()\n");

            if (!CanPulseGuide)
            {
                throw new System.Exception("PulseGuide() cannot be called if CanPuluseGuide == false");
            }

            try
            {
                lock (oGuideStateLock)
                {
                    bGuiding = true;
                }

                switch (Direction)
                {
                    case GuideDirections.guideNorth:
                        sxCamera.guideNorth(Duration);
                        break;
                    case GuideDirections.guideSouth:
                        sxCamera.guideSouth(Duration);
                        break;
                    case GuideDirections.guideEast:
                        sxCamera.guideEast(Duration);
                        break;
                    case GuideDirections.guideWest:
                        sxCamera.guideWest(Duration);
                        break;
                }
            }
            finally
            {
                lock (oGuideStateLock)
                {
                    bGuiding = false;
                }
            }
        }

        /// <summary>
        /// Sets the camera cooler setpoint in degrees Celsius, and returns the current
        /// setpoint.
        /// Note:  camera hardware and/or driver should perform cooler ramping, to prevent
        /// thermal shock and potential damage to the CCD array or cooler stack.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw exception if command not successful.</exception>
        /// <exception cref=" System.Exception">Must throw exception if CanSetCCDTemperature is False.</exception>
        public double SetCCDTemperature
        {
            get
            {
                Log.Write("SetCCDTemperature Get()\n");
                throw new System.Exception("SetCCDTemperature cannot be use if CanSetCCDTemperature == false");
            }
            set
            {
                Log.Write("SetCCDTemperature SEt()\n");
                throw new System.Exception("SetCCDTemperature cannot be use if CanSetCCDTemperature == false");
            }
        }

        abstract public void SetupDialog();
 
        internal void hardwareCapture(double Duration, bool Light)
        {
            Log.Write(String.Format("hardwareCapture({0}, {1}): begins\n", Duration, Light));

            try
            {
                exposureStart = DateTime.Now;
                
                lock (oCameraStateLock)
                {
                    if (bAbortRequested)
                    {
                        return;
                    }
                    state = CameraStates.cameraDownload;
                }

                sxCamera.delayMs = (uint)(1000 * Duration);
                sxCamera.recordPixelsDelayed();

                actualExposureLength = new TimeSpan(0, 0, 0, 0, (int)(1000*Duration));

                Log.Write("hardwareCapture(): dends\n");

                bImageValid = true;
            }
            finally
            {
                lock (oCameraStateLock)
                {
                    state = CameraStates.cameraIdle;
                }
            }
        }

        internal void softwareCapture(double Duration, bool Light)
        {
            Log.Write(String.Format("softwareCapture({0}, {1}): begins\n", Duration, Light));

            try
            {
                sxCamera.clearRecordedPixels();
                sxCamera.clearCcdPixels(); // This clears both the CCD and the recorded pixels.  For
                                           // long exposures (> 2 seconds) we will cleare the recorded pixels again just before
                                           // the exposure ends to clear any accumulated noise.
                bool bRecordedCleared = false;
                
                exposureStart = DateTime.Now;
                desiredExposureLength = TimeSpan.FromSeconds(Duration);
                DateTime exposureEnd = exposureStart + desiredExposureLength;

                // We sleep for most of the exposure, then spin for the last little bit
                // because this helps us end closer to the right time

                Log.Write("softwareCapture(): about to begin loop, exposureEnd=" + exposureEnd + "\n");
                for (TimeSpan remainingExposureTime = desiredExposureLength;
                    remainingExposureTime.TotalMilliseconds > 0;
                    remainingExposureTime = exposureEnd - DateTime.Now)
                {
                    
                    if (remainingExposureTime.TotalSeconds < 2.0 && !bRecordedCleared)
                    {
                        Log.Write("softwareCapture(): before clearRecordedPixels(), remaining exposure=" + remainingExposureTime.TotalSeconds + "\n");
                        sxCamera.clearRecordedPixels();
                        bRecordedCleared = true;
                    }
                    else if (remainingExposureTime.TotalMilliseconds > 75)
                    {
                        // sleep in small chunks so that we are responsive to abort and stop requests
                        //Log.Write("Before sleep, remaining exposure=" + remainingExposureTime.TotalSeconds + "\n");
                        Thread.Sleep(50);
                    }

                    if (bAbortRequested || bStopRequested)
                    {
                        break;
                    }
                }

                lock (oCameraStateLock)
                {
                    if (bAbortRequested)
                    {
                        return;
                    }
                    state = CameraStates.cameraDownload;
                }

                sxCamera.recordPixels(out exposureEnd);

                actualExposureLength = exposureEnd - exposureStart;

                Log.Write(String.Format("softwareCapture(): delay ends, actualExposureLength={0}, requested={1}\n", actualExposureLength.TotalSeconds, Duration));
                
                bImageValid = true;
            }
            finally
            {
                lock (oCameraStateLock)
                {
                    state = CameraStates.cameraIdle;
                }
            }
        }

        /// <summary>
        /// Starts an exposure. Use ImageReady to check when the exposure is complete.
        /// </summary>
        /// <exception cref=" System.Exception">NumX, NumY, XBin, YBin, StartX, StartY, or Duration parameters are invalid.</exception>
        /// <exception cref=" System.Exception">CanAsymmetricBin is False and BinX != BinY</exception>
        /// <exception cref=" System.Exception">the exposure cannot be started for any reason, such as a hardware or communications error</exception>
        virtual public void StartExposure(double Duration, bool Light)
        {
            StartExposure(Duration, Light, false);
        }

        protected void StartExposure(double Duration, bool Light, bool useHardwareTimer)
        {
            Log.Write(String.Format("StartExposure({0}, {1}, {2}) begins\n", Duration, Light, useHardwareTimer));

            lock (oCameraStateLock)
            {
                if (state != CameraStates.cameraIdle)
                {
                    throw new System.Exception("Exposure already in progress");
                }
                state = CameraStates.cameraExposing;
                bAbortRequested = false;
                bStopRequested = false;
                bImageValid = false;
            }

            try
            {
                sxCamera.width = (ushort)(binX * NumX);
                sxCamera.height = (ushort)(binY * NumY);
                sxCamera.xOffset = (ushort)(binX * StartX);
                sxCamera.yOffset = (ushort)(binY * StartY);

                CaptureDelegate captureDelegate;

                if (useHardwareTimer)
                {
                    captureDelegate = new CaptureDelegate(hardwareCapture);
                }
                else
                {
                    captureDelegate = new CaptureDelegate(softwareCapture);
                }

                Log.Write("StartExposure() before captureDelegate.BeginInvode()\n");

                captureDelegate.BeginInvoke(Duration, Light, null, null);

                Log.Write("StartExposure() after captureDelegate.BeginInvode()\n");
            }
            catch (Exception ex)
            {
                lock (oCameraStateLock)
                {
                    state = CameraStates.cameraIdle;
                }

                throw ex;
            }
        }

        /// <summary>
        /// Sets the subframe start position for the X axis (0 based). Also returns the
        /// current value.  If binning is active, value is in binned pixels.
        /// </summary>
        public int StartX
        {
            get;
            set;
        }

        /// <summary>
        /// Sets the subframe start position for the Y axis (0 based). Also returns the
        /// current value.  If binning is active, value is in binned pixels.
        /// </summary>
        public int StartY
        {
            get;
            set;
        }

        /// <summary>
        /// Stops the current exposure, if any.  If an exposure is in progress, the readout
        /// process is initiated.  Ignored if readout is already in process.
        /// </summary>
        /// <exception cref=" System.Exception">Must throw an exception if CanStopExposure is False</exception>
        /// <exception cref=" System.Exception">Must throw an exception if no exposure is in progress</exception>
        /// <exception cref=" System.Exception">Must throw an exception if the camera or link has an error condition</exception>
        /// <exception cref=" System.Exception">Must throw an exception if for any reason no image readout will be available.</exception>
        public void StopExposure()
        {
            Log.Write("StopExposure()\n");
            lock (oCameraStateLock)
            {
                switch (state)
                {
                    case CameraStates.cameraExposing:
                    case CameraStates.cameraDownload:
                        bStopRequested = true;
                        break;
                    default:
                        if (bStopRequested)
                            break; // they asked when it was legal and are just asking again.
                        throw new System.Exception("Stop not possible.");
                }
            }
        }

        #endregion
    }
}
