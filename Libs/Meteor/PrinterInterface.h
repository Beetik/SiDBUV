/**
    (c) Meteor Inkjet. All rights reserved.

    @file PrinterInterface.h

    Application programming interface for the Meteor print system
*/
#pragma once
#ifndef PRINTERINTERFACE_H
#define PRINTERINTERFACE_H

#ifdef _WIN32               // N.B. _WIN32 is defined for both 32 bit *and* 64 bit Windows targets
#ifdef DLL_MODULE           // Only defined in the DLL project itself
  #define IFTYPE
#else
  #define IFTYPE __declspec(dllimport)
#endif
#define CALLCONV __stdcall
#else
#define IFTYPE
#define CALLCONV
#endif

#include "Meteor.h"

extern "C"
{

// .............................................................................
// Opens connection to the print engine
// Returns RVAL_OK if successful

IFTYPE eRET CALLCONV PiOpenPrinter(void);
typedef eRET (CALLCONV fnPiOpenPrinter)(void);

// .............................................................................
// Find out if the current process has got the PrinterInterface connection open
IFTYPE bool CALLCONV PiIsProcessConnected(void);
typedef bool (CALLCONV fnPiIsProcessConnected)(void);

// .............................................................................
// Sends command to the print engine. Commands are listed in the CtrlCmdIds
// enumeration in Meteor.h

IFTYPE eRET CALLCONV PiSendCommand(uint32 *pData);
typedef eRET (CALLCONV fnPiSendCommand)(uint32 *pData);

// .............................................................................
// Extended version of send command which allows commands to be targeted
// at a head lane

IFTYPE eRET CALLCONV PiSendCommandEx(uint32 *pData, CommandExParams* pParams);
typedef eRET (CALLCONV fnPiSendCommandEx)(uint32 *pData, CommandExParams* pParams);

// .............................................................................
// Query the size of the largest command, or set of commands, which can be sent to PiSendCommand(Ex) wtihout receiving 
// an RVAL_FULL (or an RVAL_BADPARAM).
// Depending on how quickly the PrintEngine is processing commands, more buffer space may already be available after the
// commands have been sent.
// This allows an application to check that there is space for a sequence of commands it is about to send, which means it
// can avoid the need to check for RVAL_FULL for every command in the sequence.
//
// lane should be zero for a standard configuration; or the lane index (1 or 2) for a system with two independent print lanes.

IFTYPE uint32 CALLCONV PiGetCommandSpaceDwords(uint32 lane);
typedef uint32  (CALLCONV fnPiGetCommandSpaceDwords)(uint32 lane);

// .............................................................................
// Sets the state of a defined signal (I/O, PSUs etc). The signal ID parameter
// format is as follows:
// Byte 2 - Pcc Number (1 - n)
// Byte 1 - Head Number (1 - n, or 0 for the PCC I/O signals)
// Byte 0 - one of SIG_xxx defined in Meteor.h
// The HDC I/O signals are bidirectional and default to inputs. To enable a
// signal as an output the corresponding SIG_OENx signals must be set to 1.
// The PCC I/O signals are dedicated inputs and outputs, so this doesn't apply.

IFTYPE eRET CALLCONV PiSetSignal(uint32 SignalId, uint32 State);
typedef eRET (CALLCONV fnPiSetSignal)(uint32 SignalId, uint32 State);

// .............................................................................
// Blocking version of the above which waits for the signal to be set / returns 
// whether there was an error (e.g. if the parameter is out of range)
// Note that (depending on the signal) this does not guarantee the signal has 
// been set in the hardware - just that the PrintEngine has queued the change
IFTYPE eRET CALLCONV PiSetAndValidateSignal(uint32 SignalId, uint32 State);
typedef eRET (CALLCONV fnPiSetAndValidateSignal)(uint32 SignalId, uint32 State);

// .............................................................................
// Sets the state of one of the adjustment parameters for head position,
// head voltage etc. The Parameter ID format is as follows:
// Byte 2 - Pcc Number (1 - n)
// Byte 1 - Head Number (1 - n, or 0 for the PCC I/O signals)
// Byte 0 - one of CCP_xxx defined in Meteor.h
// The parameters are adjustment values (offsets) from the default values which
// are specified in the config file

IFTYPE eRET CALLCONV PiSetParam(uint32 ParamId, uint32 Value);
typedef eRET (CALLCONV fnPiSetParam)(uint32 ParamId, uint32 Value);

// .............................................................................
// As above, except that the function blocks and does not return until the 
// parameter has been set in Meteor.  This function may fail in circumstances 
// where PiSetParam would succeed - e.g. if there is an attempt to set a 
// parameter with an out of range value

IFTYPE eRET CALLCONV PiSetAndValidateParam(uint32 ParamId, uint32 Value);
typedef eRET (CALLCONV fnPiSetAndValidateParam)(uint32 ParamId, uint32 Value);

// .............................................................................
// Turns the head power on or off, depending on the state parameter (zero = off,
// non-zero = on)

IFTYPE eRET CALLCONV PiSetHeadPower(uint32 State);
typedef eRET (CALLCONV fnPiSetHeadPower)(uint32 State);

// .............................................................................
// Used with scanning printers when the carriage is at a home sensor. Zeros the
// Absolute X-counters on all Master PCCs

IFTYPE eRET CALLCONV PiSetHome(void);
typedef eRET (CALLCONV fnPiSetHome)(void);

// .............................................................................
// Used with scanning printers to update the printer resolution.
// The encoder multiplier/divider values to use are changed using PiSetParam.
// This re-initialises all head X counters, while keeping the original home 
// position.  It can be called when the printer carriage is in any position
// provided it is stationary.
//
// This function operates synchronously with Meteor - i.e. it is safe to move
// the printer carriage as soon as the function has returned.

IFTYPE eRET CALLCONV PiUpdateEncoderResolution(void);
typedef eRET (CALLCONV fnPiUpdateEncoderResolution)(void);

// .............................................................................
// Writes the head x-offsets to the PCC registers, allowing on-the-fly adjustment
// Prior to calling this method, the desired head x-offset values should be
// set via PiSetParam.  The new offsets will take effect on the next
// product detect.

IFTYPE eRET CALLCONV PiUpdateXoffsets(void);
typedef eRET (CALLCONV fnPiUpdateXoffsets)(void);

// .............................................................................
// Forces Meteor to Re-send the PCC configuration settings

IFTYPE eRET CALLCONV PiReSendPCCConfiguration(void);
typedef eRET (CALLCONV fnPiReSendPCCConfiguration)(void);

// .............................................................................
// Sends commands to the PDC-K stepper motor. This command may block for up to 
// 15 seconds.

IFTYPE eRET CALLCONV PiStepperMotor(uint32 pccnum, uint32 hnum, uint32 StepReg);
typedef eRET (CALLCONV fnPiStepperMotor)(uint32 pccnum, uint32 hnum, uint32 StepReg);

// .............................................................................
// Function to return details of the current Meteor configuration
IFTYPE TCurrentCfgDesc* CALLCONV PiGetCurrentCfgDesc(void);
typedef TCurrentCfgDesc* (CALLCONV fnPiGetCurrentCfgDesc(void));


// .............................................................................
// Clears all print data buffers in the print engine (for both Preload and Fifo
// data paths), effectively aborting the current job.
//
// After a call to PiAbort the print engine becomes busy until the abort
// has fully completed.  This can be tested using PiIsBusy.
//
// After issuing an abort, an application should send no further print commands
// until PiIsBusy returns false.

IFTYPE eRET CALLCONV PiAbort(void);
typedef eRET (CALLCONV fnPiAbort)(void);

// .............................................................................
// Halt the print lane.  This is similar to PiAbort but can be used in a
// multi-lane system to abort a job on one print lane without affecting
// in-progress jobs on the other lanes.  Lanes are indexed from 1.

IFTYPE eRET CALLCONV PiHaltPrintLane(uint32 lane);
typedef eRET (CALLCONV fnPiHaltPrintLane)(uint32 lane);

// .............................................................................
// Disconnects from the print engine and deletes allocated memory. Returns
// RVAL_BUSY if can't close because print data buffers are still in use, or
// RVAL_OK if close succeeds. The application should continually call
// this function until it returns RVAL_OK, or should call PiAbort() to clear the
// buffers, followed by a call to PiClosePrinter()

IFTYPE eRET CALLCONV PiClosePrinter(void);
typedef eRET (CALLCONV fnPiClosePrinter)(void);

// .............................................................................
// Clears current palette information for the given id.
IFTYPE uint32 CALLCONV PiClearPalette(uint32 id, uint32 p);
typedef uint32 (CALLCONV fnPiClearPalette)(uint32 id, uint32 p);

// .............................................................................
// Test in advance whether a call to PiClosePrinter will succeed
// This function does not yield so should be called no more frequently than 
// once every 100ms
IFTYPE bool CALLCONV PiCanClosePrinter(void);
typedef bool (CALLCONV fnPiCanClosePrinter)(void);

// .............................................................................
// Returns the next log message from the thread-safe message queue. The queue
// contains useful debug information - the content will vary as the development
// progresses. The log message is NULL-terminated. If the queue is empty, the
// function returns RVAL_EMPTY, otherwise returns RVAL_OK.
// This API is only for messages generated by the PrinterInterface.
// Use PiGetPrintEngineLog for messages generated by the PrintEngine itself.

IFTYPE eRET CALLCONV PiGetLogMessage(char *Buff, uint32 BuffSize);
typedef eRET (CALLCONV fnPiGetLogMessage)(char *Buff, uint32 BuffSize);

// .............................................................................
// Returns the next error message from the thread-safe message queue. The queue
// contains useful debug information - the content will vary as the development
// progresses. The log message is NULL-terminated. If the queue is empty, the
// function returns RVAL_EMPTY, otherwise returns RVAL_OK.

IFTYPE eRET CALLCONV PiGetPrintEngineError(char *Buff, uint32 BuffSize);
typedef eRET (CALLCONV fnPiGetPrintEngineError)(char *Buff, uint32 BuffSize);

// .............................................................................
// Tests whether print engine is still processing the last control command
// The print engine becomes busy after calls to the following interface methods:
//  PiAbort, PiSetSignal, PiSetParam, PiSetHeadPower,
//  PiSetHome, PiSendMonitorCommand, PiSetWaveform
// Note that the print engine does **not** become busy after a call to 
// PiSendCommand.
// All application status requests (PiGetPrnStatus, PiGetPccStatus, 
// PiGetHeadStatus etc.) will return NULL while the printer interface is busy

IFTYPE bool CALLCONV PiIsBusy(void);
typedef bool (CALLCONV fnPiIsBusy)(void);

// .............................................................................
// Set a user waveform file

IFTYPE eRET CALLCONV PiSetWaveform(uint32 PccNum, uint32 HeadNum, uint32 Index, char *WaveformFile, uint32 Row);
typedef eRET (CALLCONV fnPiSetWaveform)(uint32 PccNum, uint32 HeadNum, uint32 Index, char *WaveformFile, uint32 Row);

// .............................................................................
// Set a delay file

IFTYPE eRET CALLCONV PiSetUnitDelays(uint32 PccNum, uint32 HeadNum, char* DelayFile, uint32 Unit);
typedef eRET(CALLCONV fnPiSetUnitDelays)(uint32 PccNum, uint32 HeadNum, char* DelayFile, uint32 Unit);

// .............................................................................
// Get a delay file

IFTYPE eRET CALLCONV PiGetUnitDelays(uint32 PccNum, uint32 HeadNum, char* DelayFilePath, uint32 Unit);
typedef eRET(CALLCONV fnPiGetUnitDelays)(uint32 PccNum, uint32 HeadNum, char* DelayFilePath, uint32 Unit);

// .............................................................................
// Forces a download of all waveforms to each head, currently only used for Vista heads.
IFTYPE eRET CALLCONV PiDownloadWaveforms(uint32 PccNum);
typedef eRET (CALLCONV fnPiDownloadWaveforms)(uint32 PccNum);

// .............................................................................
// Sends a command to Monitor

IFTYPE eRET CALLCONV PiSendMonitorCommand(eMONITORCMD cmd);
typedef eRET (CALLCONV fnPiSendMonitorCommand)(eMONITORCMD cmd);

// .............................................................................
// Starts Meteor print engine hosted in the print app process

IFTYPE eRET CALLCONV PiStartPrintEngine(const char* pConfigFile);
typedef eRET (CALLCONV fnPiStartPrintEngine)(const char* pConfigFile);

// .............................................................................
// Stops Meteor print engine hosted in the print app process
// Returns: RVAL_NO_PRINTER if PrintEngine is not running. RVAL_BADSEQ if 
// PrintEngine can't stop because it is in a job (if dwForce is true, then 
// this will succeed). RVAL_BUSY if the command could not be processed, try again.
// RVAL_OK, if the command succeeded.

IFTYPE eRET CALLCONV PiStopPrintEngine(uint32 dwForce);
typedef eRET (CALLCONV fnPiStopPrintEngine)(uint32 dwForce);

// .............................................................................
// Fetches next log message from print engine
// Requires [Test] LogToApp = 1 in the Meteor configuration file

IFTYPE eRET CALLCONV PiGetPrintEngineLog(char *buff, uint32 buffsize);
typedef eRET (CALLCONV fnPiGetPrintEngineLog)(char *buff, uint32 buffsize);

// .............................................................................
// Set the log modules which are enabled in the PrintEngine
// Format follows the LogModules config file entry
IFTYPE eRET CALLCONV PiSetLogModules(const char* pModules);
typedef eRET (CALLCONV fnPiSetLogModules)(const char* pModules);

// .............................................................................
// Get structure describing continuous print state

IFTYPE TContinuousPrintStatus* CALLCONV PiGetContinuousPrintStatus();
typedef TContinuousPrintStatus* (CALLCONV fnPiGetContinuousPrintStatus)();

// .............................................................................
// If the config file contains a Parameters section, this function will 
// send all of the parameters in the section and return RVAL_OK. If there
// is no parameters section, or the section is empty, it will return RVAL_NOTUSED.

IFTYPE eRET CALLCONV PiSendParametersFromConfig();
typedef eRET (CALLCONV fnPiSendParametersFromConfig)();

// .............................................................................
// Re-reads the config file from disk, and updates the Xoffsets and/or the
// Yoffsets from the [ControllerX] sections.  The new offsets take effect when
// the next print job is started.  Waveform filenames can also be re-loaded.
// Set BM_CFG_RELOAD_XOFFSETS in flags to reload all X offsets
// Set BM_CFG_RELOAD_YOFFSETS in flags to reload all Y offsets
// Set BM_CFG_RELOAD_WFMS in flags to reload all waveform file parameters

IFTYPE eRET CALLCONV PiReloadConfigValues(uint32 flags);
typedef eRET (CALLCONV fnPiReloadConfigValues)(uint32 flags);

// .............................................................................
// Retrieves the details of Meteor's current plane configuration
IFTYPE const MeteorPlaneConfig* CALLCONV PiGetPlaneConfig();
typedef const MeteorPlaneConfig* (CALLCONV fnPiGetPlaneConfig)(); 

// .............................................................................
// Get version number of the printer interface dll
IFTYPE uint32 CALLCONV PiGetVersion();

// .............................................................................
// Get build number of the printer interface dll
IFTYPE uint32 CALLCONV PiGetBuildNumber();

// .............................................................................
// Set path to the PrintEngine log file.  Send NULL or "" to turn off disk logging.
// path must be NULL terminated and a maximum of 256 characters including the
// NULL terminator
IFTYPE eRET CALLCONV PiSetPrintEngineLogPath(const char* path);

// .............................................................................
// Set path to the SimFile folder.  Send NULL or "" to disable SimPrint file
// creation.
// path must be NULL terminated and a maximum of 256 characters including the
// NULL terminator
IFTYPE eRET CALLCONV PiSetSimFileFolder(const char* path);

// .............................................................................
// Allocate an image buffer.  Only available when hosting the PrintEngine.
// Returns an image buffer handle, or 0xFFFFFFFF if allocation failed or if
// the PrintEngine is not being hosted by the calling application.
IFTYPE uint32 CALLCONV PiAllocateImageBuffer(uint32 sizeDWORDs);

// .............................................................................
// Copy data into the image buffer previously allocated by PiAllocateImageBuffer.
// Only available when the calling application is also hosting the PrintEngine.
IFTYPE eRET PiFillImageBuffer(uint32 bufferID, uint32 offsetDWORDs, uint32 sizeDWORDs, uint32* pData);

// .............................................................................
// Allocate an image buffer and load it with the contents of the TIFF file.
// tiffPath must be an absolute path to the file.  This method supports a
// limited subset of TIFF formats.  The file must be the same resolution
// (bits per pixel) as Meteor's currently selected bits per pixel unless
// TIFF_BPP_ANY is set in the flags
// The ImageLoadDetails structure provides additional information about how
// to load the file (e.g. cropping co-ordinates) and can be null if the 
// entire file is being loaded.
// If RVAL_MEM_LIMIT is returned, the PrintEngine memory limit for TIFF buffers 
// has been reached; this limit can be changed using the [Test] MaxTiffBufferMb 
// configuration file entry
#ifdef _WIN32
IFTYPE eRET PiAllocateTiffImageBuffer(const wchar_t* tiffPath, uint32 flags, TIFFImageDetails* bufferDetails, ImageLoadDetails* loadDetails);
#else
IFTYPE eRET PiAllocateTiffImageBufferA(const char* tiffPath, uint32 flags, TIFFImageDetails* bufferDetails, ImageLoadDetails* loadDetails);
#endif
// .............................................................................
// Allocate multiple images buffers and load them with print data from the file
// filePath is an absolute path to the file
// pDetails points to a structure describing how the print data is laid out in
// the file
// planes is a bitmask selecting which planes to use.  BIT0 is plane 1 etc.
// bufferIndexes points to an array which is filled in with the indexes
// of the image buffers for each plane, index 0 is the buffer for plane 1 etc.
// the array must have MAX_MULTICHANNEL_FILE_BUFFERS elements
// The ImageLoadDetails structure provides additional information about how
// to load the file (e.g. cropping co-ordinates) and can be null if the 
// entire file is being loaded.
#ifdef _WIN32
IFTYPE eRET PiAllocateMultichannelFileBuffers(const wchar_t* filePath, MultichannelFileDetails* pDetails, uint32 planes, uint32* bufferIndexes, ImageLoadDetails* loadDetails);
#else
IFTYPE eRET PiAllocateMultichannelFileBuffersA(const char* filePath, MultichannelFileDetails* pDetails, uint32 planes, uint32* bufferIndexes, ImageLoadDetails* loadDetails);
#endif

// .............................................................................
// 
#ifdef _WIN32
// .............................................................................
// RIP the file into a set of single channel image buffers ready for printing
IFTYPE eRET PiRipFile(const wchar_t* filePath, RipDetails* pRipDetails, ImageLoadDetails* pLoadDetails);
#endif

// .............................................................................
// 
#ifdef _WIN32
// .............................................................................
// Generate a preview thumbnail for a file which needs to be RIPPed prior to 
// printing (e.g. pdf, RGB TIFF).
IFTYPE eRET PiRipPreview(const wchar_t* filePath, RipPreviewDetails* pPreviewDetails);
#endif


// .............................................................................
// 
#ifdef _WIN32
// .............................................................................
// Generate a QR code in an image buffer
IFTYPE eRET PiCreateQrCode(const wchar_t* text, QrCodeDetails* pDetails);
#endif

// .............................................................................
// Fill in the details (width, height etc.) of the image file, if it is a 
// type which is recognised by the PrintEngine.
// Should be used only when the calling application is also hosting the PrintEngine;
// it is valid to call PiGetImageFileDetails before the call to PiStartPrintEngine
#ifdef _WIN32
IFTYPE eRET PiGetImageFileDetails(const wchar_t* filePath, GenericImageFileDetails* pDetails);
#else
IFTYPE eRET PiGetImageFileDetailsA(const char* filePath, GenericImageFileDetails* pDetails);
#endif

// .............................................................................
// Calculate the pixel counts for each grey level for the image in the buffer,
// previously loaded by PiAllocateTiffImageBuffer or PiAllocateMultichannelFileBuffers
IFTYPE eRET PiCalculateImageBufferPixelCounts(uint32 bufferId, ImagePixelDetails* pPxDetails);

// .............................................................................
// Extended API to set and store parameters. 
IFTYPE eRET CALLCONV PiSetParamEx(uint32 aPEAddr, uint32 aParamId, uint64 aParamVal);

// .............................................................................
// Extended API to retrieve parameters. 
IFTYPE eRET CALLCONV PiGetParamEx(uint32 aPEAddr, uint32 aParamId, uint64* apParamVal);

// .............................................................................
// Reload a specified parameter from a config file. Use case: some application changes a selected 
// parameter in the Meteor config file directly and this API call needed to tell PrintEngine to reload its value.
IFTYPE eRET CALLCONV PiReloadParamEx(uint32 aParamId, uint32 aFlags);

// .............................................................................
// API so set and retrieve PrintEngine string parameters
IFTYPE eRET CALLCONV PiSetStringEx(uint32 aStrPoolId, uint32 aStrIdx, const char* apStr, uint64 aReserved);
IFTYPE eRET CALLCONV PiGetStringEx(uint32 aStrPoolId, uint32 aStrIdx, void* apOutBuf, uint32 aBufSize, uint64* apReserved);
// .............................................................................

// .............................................................................
// Extended API to retrieve status data. 
IFTYPE eRET CALLCONV PiGetStatusEx(uint32 aPEAddr, eSTATUSTYPE aStatreq, void* apOutBuf, uint32 aBufSize);

// .............................................................................
// EEPROM control API
IFTYPE eRET CALLCONV PiEepromControl(uint32 aPEAddr, TEepromCtrl* apEECtrl);





//###########################################################################
//#     These API functions that return pointers to the status structures from the internals 
//#     of the PrintEngine are not thread safe and will be deprecated eventually.
//#     It is much better to use PiGetStatusEx() that allows PCC:HDC:HEAD:JA addressing and
//#     return copies of the status data structures, therefore thread-safe
//###########################################################################

// .............................................................................
// Requests the print-engine update the printer status structure, and returns
// a pointer to that structure. Returns NULL if the print engine does not
// respond to the request

IFTYPE TAppStatus* CALLCONV PiGetPrnStatus(void);
typedef TAppStatus* (CALLCONV fnPiGetPrnStatus)(void);

// .............................................................................
// Requests the print-engine update the PCC status structure with information
// for the specified PCC head. Returns a pointer to that structure, or NULL if
// the print engine does not respond to the request. pccnum is 1 - n

IFTYPE TAppPccStatus* CALLCONV PiGetPccStatus(uint32 pccnum);
typedef TAppPccStatus* (CALLCONV fnPiGetPccStatus)(uint32 pccnum);

// .............................................................................
// Requests the print-engine update the head status structure with information
// for the specified print head. Returns a pointer to that structure, or NULL if
// the print engine does not respond to the request. pccnum and hnum are 1 - n

IFTYPE TAppHeadStatus* CALLCONV PiGetHeadStatus(uint32 pccnum, uint32 hnum);
typedef TAppHeadStatus* (CALLCONV fnPiGetHeadStatus)(uint32 pccnum, uint32 hnum);

// .............................................................................
// Requests the print-engine update the Eeprom Structure with information for the 
// specified print head. Returns a pointer to that structure or NULL if the print engine
// does not respond to the request. pccnum and hnum are 1 - n

IFTYPE TAppQClassEeprom* CALLCONV PiGetQClassEeprom(uint32 pccnum, uint32 hnum);
typedef TAppQClassEeprom* (CALLCONV fnPiGetQClassEeprom)(uint32 pccnum, uint32 hnum);

// .............................................................................
// Requests an update of the compact-flash status structure, waits for
// the response and returns a pointer to the TCompactFlashStatus structure.
// The structure is defined in Meteor.h

IFTYPE TCompactFlashStatus* CALLCONV PiGetCompactFlashStatus(void);
typedef TCompactFlashStatus* (CALLCONV fnPiGetCompactFlashStatus)(void);

// .............................................................................
// Returns the last error code from a status request from the calling thread
// (PiGetPrnStatus, PiGetPccStatus, PiGetHeadStatus etc.)
//
// Can be used if a call to a status request API function returns NULL, to find 
// out more information.
//
// Returns:
//  RVAL_BADSEQ if the PrinterInterface is not open
//  RVAL_NOT_USED if there has never been a status request on the calling thread
//  RVAL_BUSY if there was a timeout requesting the status
//   (in which case the application should retry the status request)
//  RVAL_NO_PRINTER if the PrintEngine has been closed
//
IFTYPE eRET CALLCONV PiGetLastStatusError(void);
typedef eRET (CALLCONV fnPiGetLastStatusError)(void);

// .............................................................................
// Get structure describing Lexmark head state

IFTYPE TLexmarkStatus* CALLCONV PiGetLexmarkStatus(uint32 pccnum, uint32 hnum);
typedef TLexmarkStatus* (CALLCONV fnPiGetLexmarkStatus)(uint32 pccnum, uint32 hnum);

// .............................................................................
// Requests the Eeprom data retrieved from the SG1024 head.  Returns a pointer to 
// that structure or NULL if the print engine does not respond to the request. 
// pccnum and hnum are 1 to n.  If TAppSG1024Eeprom.DataReady is not set the
// print engine is still retrieving the Eeprom information and the application
// should try again later.

IFTYPE TAppSG1024Eeprom* CALLCONV PiGetSG1024Eeprom(uint32 pccnum, uint32 hnum);
typedef TAppSG1024Eeprom* (CALLCONV fnPiGetSG1024Eeprom)(uint32 pccnum, uint32 hnum);

// .............................................................................
// Requests the Eeprom data retrieved from the RG4 head.  Returns a pointer to 
// that structure or NULL if the print engine does not respond to the request. 
// pccnum and hnum are 1 to n. If TAppRG4HDCEeprom.DataReady is not set the
// print engine is still retrieving the Eeprom information and the application
// should try again later.

IFTYPE TAppRG4HDCEeprom* CALLCONV PiGetRG4HDCEeprom(uint32 pccnum, uint32 hnum);
typedef TAppRG4HDCEeprom* (CALLCONV fnPiGetRG4HDCEeprom)(uint32 pccnum, uint32 hnum);

// .............................................................................
// Requests the Eeprom data retrieved from the RG4 head.  Returns a pointer to 
// that structure or NULL if the print engine does not respond to the request. 
// pccnum and hnum are 1 to n. If TAppRG5Eeprom.DataReady is not set the
// print engine is still retrieving the Eeprom information and the application
// should try again later.

IFTYPE TAppRG5Eeprom* CALLCONV PiGetRG5Eeprom(uint32 pccnum, uint32 hnum);
typedef TAppRG5Eeprom* (CALLCONV fnPiGetRG5Eeprom)(uint32 pccnum, uint32 hnum);

// .............................................................................
// Requests the Eeprom data from the head attached to the given PCC and HDC.
// Returns the pointer to the buffer containing parsed EEPROM data in JSON
// format or NULL if the print engine does not respond to the request.
// pccnum and hnum are 1 to n.
// EEPROM data fields are head-specific. Client is responsible for parsing JSON and
// correct interpretation of its contents.
IFTYPE const char* CALLCONV PiGetEepromData(uint32 pccnum, uint32 hnum);

// .............................................................................
// Get structure containing the HDC serial number
IFTYPE SerialNumberStatus* CALLCONV PiGetHDCSerialNum(uint32 pccnum, uint32 hnum);
typedef SerialNumberStatus* (CALLCONV fnPiGetHDCSerialNum)(uint32 pccnum, uint32 hnum);

// .............................................................................
// Get structure containing the PDC serial number
IFTYPE SerialNumberStatus* CALLCONV PiGetPDCSerialNum(uint32 pccnum);
typedef SerialNumberStatus* (CALLCONV fnPiGetPDCSerialNum)(uint32 pccnum);


} // end extern "C"

#endif // PRINTERINTERFACE_H
