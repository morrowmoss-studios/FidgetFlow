#import <AVFoundation/AVFoundation.h>

extern "C"
{
    void FidgetFlow_EnableMixedRecording()
    {
        AVAudioSession *session =
            [AVAudioSession sharedInstance];

        NSError *error = nil;

        AVAudioSessionCategoryOptions options =
            AVAudioSessionCategoryOptionMixWithOthers |
            AVAudioSessionCategoryOptionDefaultToSpeaker;

        BOOL categorySuccess =
            [session setCategory:AVAudioSessionCategoryPlayAndRecord
                     withOptions:options
                           error:&error];

        if (!categorySuccess)
        {
            NSLog(
                @"[FidgetFlowAudioSession] Failed to set category: %@",
                error
            );

            return;
        }

        error = nil;

        BOOL activeSuccess =
            [session setActive:YES
                         error:&error];

        if (!activeSuccess)
        {
            NSLog(
                @"[FidgetFlowAudioSession] Failed to activate session: %@",
                error
            );

            return;
        }

        NSLog(
            @"[FidgetFlowAudioSession] Mixed recording session active."
        );
    }
}