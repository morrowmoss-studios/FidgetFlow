#import <AVFoundation/AVFoundation.h>
#import <Foundation/Foundation.h>

static AVAudioEngine *ffAudioEngine = nil;
static NSObject *ffBufferLock = nil;

static const int FF_BUFFER_SIZE = 8192;
static float ffSampleBuffer[FF_BUFFER_SIZE];
static int ffWriteIndex = 0;


// ---------------------------------------------------------
// DEBUG
// ---------------------------------------------------------

static void FidgetFlow_LogSession(NSString *stage)
{
    AVAudioSession *session =
        [AVAudioSession sharedInstance];

    NSLog(
        @"[FidgetFlowAudio] %@ | Category=%@ | Mode=%@ | Options=%lu | OtherAudio=%@ | Route=%@",
        stage,
        session.category,
        session.mode,
        (unsigned long)session.categoryOptions,
        session.otherAudioPlaying ? @"YES" : @"NO",
        session.currentRoute
    );
}


// ---------------------------------------------------------
// START MICROPHONE
// ---------------------------------------------------------

extern "C"
{
    bool FidgetFlow_StartMicrophone()
    {
        NSLog(
            @"[FidgetFlowAudio] StartMicrophone requested."
        );

        AVAudioSession *session =
            [AVAudioSession sharedInstance];

        NSError *error = nil;

        FidgetFlow_LogSession(
            @"BEFORE DEACTIVATION"
        );


        // -------------------------------------------------
        // 1. STOP ANY EXISTING ENGINE
        // -------------------------------------------------

        if (ffAudioEngine != nil)
        {
            AVAudioInputNode *oldInput =
                ffAudioEngine.inputNode;

            if (oldInput != nil)
            {
                [oldInput removeTapOnBus:0];
            }

            [ffAudioEngine stop];

            ffAudioEngine = nil;
        }


        // -------------------------------------------------
        // 2. DEACTIVATE CURRENT UNITY AUDIO SESSION
        // -------------------------------------------------

        error = nil;

        BOOL deactivated =
            [session
                setActive:NO
                withOptions:
                    AVAudioSessionSetActiveOptionNotifyOthersOnDeactivation
                error:&error
            ];

        if (!deactivated)
        {
            NSLog(
                @"[FidgetFlowAudio] Initial deactivation warning: %@",
                error
            );

            // Do not abort here.
            // iOS can legitimately refuse a redundant deactivation.
        }
        else
        {
            NSLog(
                @"[FidgetFlowAudio] Previous session deactivated."
            );
        }

        FidgetFlow_LogSession(
            @"AFTER DEACTIVATION"
        );


        // -------------------------------------------------
        // 3. CONFIGURE MIXABLE RECORDING WHILE INACTIVE
        // -------------------------------------------------

        error = nil;

        AVAudioSessionCategoryOptions options =
            AVAudioSessionCategoryOptionMixWithOthers |
            AVAudioSessionCategoryOptionDefaultToSpeaker;

        BOOL categorySuccess =
            [session
                setCategory:
                    AVAudioSessionCategoryPlayAndRecord
                mode:
                    AVAudioSessionModeDefault
                options:
                    options
                error:
                    &error
            ];

        if (!categorySuccess)
        {
            NSLog(
                @"[FidgetFlowAudio] setCategory failed: %@",
                error
            );

            return false;
        }

        NSLog(
            @"[FidgetFlowAudio] PlayAndRecord + MixWithOthers configured."
        );

        FidgetFlow_LogSession(
            @"AFTER CATEGORY CONFIGURATION"
        );


        // -------------------------------------------------
        // 4. REACTIVATE OUR NEW MIXABLE SESSION
        // -------------------------------------------------

        error = nil;

        BOOL activeSuccess =
            [session
                setActive:YES
                withOptions:0
                error:&error
            ];

        if (!activeSuccess)
        {
            NSLog(
                @"[FidgetFlowAudio] Recording session activation failed: %@",
                error
            );

            return false;
        }

        NSLog(
            @"[FidgetFlowAudio] Mixed recording session activated."
        );

        FidgetFlow_LogSession(
            @"AFTER REACTIVATION"
        );


        // -------------------------------------------------
        // 5. MAKE SURE AUDIO ROUTES THROUGH SPEAKER
        // -------------------------------------------------

        error = nil;

        BOOL speakerSuccess =
            [session
                overrideOutputAudioPort:
                    AVAudioSessionPortOverrideSpeaker
                error:
                    &error
            ];

        if (!speakerSuccess)
        {
            NSLog(
                @"[FidgetFlowAudio] Speaker override warning: %@",
                error
            );
        }
        else
        {
            NSLog(
                @"[FidgetFlowAudio] Speaker route active."
            );
        }


        // -------------------------------------------------
        // 6. CREATE NATIVE AUDIO ENGINE
        // -------------------------------------------------

        if (ffBufferLock == nil)
        {
            ffBufferLock =
                [[NSObject alloc] init];
        }

        @synchronized(ffBufferLock)
        {
            ffWriteIndex = 0;

            for (
                int i = 0;
                i < FF_BUFFER_SIZE;
                i++
            )
            {
                ffSampleBuffer[i] = 0.0f;
            }
        }

        ffAudioEngine =
            [[AVAudioEngine alloc] init];

        AVAudioInputNode *inputNode =
            ffAudioEngine.inputNode;

        if (inputNode == nil)
        {
            NSLog(
                @"[FidgetFlowAudio] ERROR: inputNode is nil."
            );

            ffAudioEngine = nil;

            return false;
        }

        AVAudioFormat *format =
            [inputNode outputFormatForBus:0];

        if (
            format == nil ||
            format.sampleRate <= 0 ||
            format.channelCount == 0
        )
        {
            NSLog(
                @"[FidgetFlowAudio] ERROR: Invalid microphone format."
            );

            ffAudioEngine = nil;

            return false;
        }

        NSLog(
            @"[FidgetFlowAudio] Mic format | Rate=%.2f | Channels=%u",
            format.sampleRate,
            (unsigned int)format.channelCount
        );


        // -------------------------------------------------
        // 7. INSTALL MICROPHONE TAP
        // -------------------------------------------------

        [inputNode removeTapOnBus:0];

        [inputNode
            installTapOnBus:0
            bufferSize:1024
            format:format
            block:^(
                AVAudioPCMBuffer *buffer,
                AVAudioTime *when
            )
            {
                if (
                    buffer == nil ||
                    buffer.floatChannelData == nil ||
                    buffer.frameLength == 0
                )
                {
                    return;
                }

                float *samples =
                    buffer.floatChannelData[0];

                int frameCount =
                    (int)buffer.frameLength;

                @synchronized(ffBufferLock)
                {
                    for (
                        int i = 0;
                        i < frameCount;
                        i++
                    )
                    {
                        ffSampleBuffer[
                            ffWriteIndex
                        ] =
                            samples[i];

                        ffWriteIndex =
                            (
                                ffWriteIndex + 1
                            ) %
                            FF_BUFFER_SIZE;
                    }
                }
            }
        ];


        // -------------------------------------------------
        // 8. START ENGINE
        // -------------------------------------------------

        [ffAudioEngine prepare];

        error = nil;

        BOOL engineStarted =
            [ffAudioEngine
                startAndReturnError:&error
            ];

        if (!engineStarted)
        {
            NSLog(
                @"[FidgetFlowAudio] AVAudioEngine start failed: %@",
                error
            );

            [inputNode removeTapOnBus:0];

            ffAudioEngine = nil;

            return false;
        }

        NSLog(
            @"[FidgetFlowAudio] Native mixed microphone STARTED."
        );

        FidgetFlow_LogSession(
            @"MIC FULLY RUNNING"
        );

        return true;
    }


    // -----------------------------------------------------
    // STOP MICROPHONE
    // -----------------------------------------------------

    void FidgetFlow_StopMicrophone()
    {
        NSLog(
            @"[FidgetFlowAudio] StopMicrophone requested."
        );

        AVAudioSession *session =
            [AVAudioSession sharedInstance];

        NSError *error = nil;


        // -------------------------------------------------
        // 1. STOP ENGINE
        // -------------------------------------------------

        if (ffAudioEngine != nil)
        {
            AVAudioInputNode *inputNode =
                ffAudioEngine.inputNode;

            if (inputNode != nil)
            {
                [inputNode removeTapOnBus:0];
            }

            [ffAudioEngine stop];

            ffAudioEngine = nil;

            NSLog(
                @"[FidgetFlowAudio] Native microphone engine stopped."
            );
        }


        // -------------------------------------------------
        // 2. DEACTIVATE RECORDING SESSION
        // -------------------------------------------------

        error = nil;

        BOOL deactivated =
            [session
                setActive:NO
                withOptions:
                    AVAudioSessionSetActiveOptionNotifyOthersOnDeactivation
                error:&error
            ];

        if (!deactivated)
        {
            NSLog(
                @"[FidgetFlowAudio] Recording session deactivation warning: %@",
                error
            );
        }
        else
        {
            NSLog(
                @"[FidgetFlowAudio] Recording session deactivated."
            );
        }


        // -------------------------------------------------
        // 3. RESTORE NORMAL UNITY-FRIENDLY MIXING CATEGORY
        // -------------------------------------------------

        error = nil;

        BOOL restoreSuccess =
            [session
                setCategory:
                    AVAudioSessionCategoryAmbient
                mode:
                    AVAudioSessionModeDefault
                options:
                    AVAudioSessionCategoryOptionMixWithOthers
                error:
                    &error
            ];

        if (!restoreSuccess)
        {
            NSLog(
                @"[FidgetFlowAudio] Ambient restore warning: %@",
                error
            );
        }
        else
        {
            NSLog(
                @"[FidgetFlowAudio] Restored Ambient + MixWithOthers."
            );
        }

        FidgetFlow_LogSession(
            @"AFTER MIC STOP"
        );
    }


    // -----------------------------------------------------
    // GET MIC SAMPLES
    // -----------------------------------------------------

    int FidgetFlow_GetMicrophoneSamples(
        float *destination,
        int sampleCount
    )
    {
        if (
            destination == NULL ||
            sampleCount <= 0 ||
            ffAudioEngine == nil
        )
        {
            return 0;
        }

        if (sampleCount > FF_BUFFER_SIZE)
        {
            sampleCount =
                FF_BUFFER_SIZE;
        }

        @synchronized(ffBufferLock)
        {
            int startIndex =
                ffWriteIndex -
                sampleCount;

            while (startIndex < 0)
            {
                startIndex +=
                    FF_BUFFER_SIZE;
            }

            for (
                int i = 0;
                i < sampleCount;
                i++
            )
            {
                int index =
                    (
                        startIndex +
                        i
                    ) %
                    FF_BUFFER_SIZE;

                destination[i] =
                    ffSampleBuffer[index];
            }
        }

        return sampleCount;
    }
}
