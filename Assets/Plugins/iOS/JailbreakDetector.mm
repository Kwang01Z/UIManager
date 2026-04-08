// File: JailbreakDetector.mm (thay thế hàm cũ)
#import <Foundation/Foundation.h>
#include <unistd.h>
#include <sys/stat.h>
#include <dlfcn.h>
#include <mach-o/dyld.h>
#include <stdio.h>
#include <string.h>
#include <errno.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/wait.h>

// Flag bitmask
enum {
    JB_NONE = 0,
    JB_PATH_FOUND = 1<<0,
    JB_WRITE_TEST = 1<<1,
    JB_ACCESS_TEST = 1<<2,
    JB_DYLD_TEST = 1<<3,
    JB_USER_APPS = 1<<4,
    JB_ENV_VAR = 1<<5,
    JB_EXCEPTION = 1<<6
};

extern "C" int IsDeviceJailbrokenDetailed()
{
    int flags = JB_NONE;
    @try {
        // 1) paths
        const char *jailbreakPaths[] = {
            "/Applications/Cydia.app",
            "/Library/MobileSubstrate/MobileSubstrate.dylib",
            "/bin/bash",
            "/usr/sbin/sshd",
            "/etc/apt",
            "/private/var/lib/apt",
            "/usr/libexec/ssh-keysign",
            NULL
        };
        for (int i = 0; jailbreakPaths[i] != NULL; i++) {
            struct stat st;
            if (stat(jailbreakPaths[i], &st) == 0) {
                NSLog(@"[JailbreakDetector] PATH FOUND: %s", jailbreakPaths[i]);
                flags |= JB_PATH_FOUND;
            }
        }

        // 2) write test
        const char *testPath = "/private/.jbtest.txt";
        FILE *f = fopen(testPath, "w");
        if (f != NULL) {
            const char *payload = "jbtest";
            size_t written = fwrite(payload, 1, strlen(payload), f);
            if (written == strlen(payload) && fflush(f) == 0) {
                int fd = fileno(f);
                if (fd != -1 && fsync(fd) == 0) {
                    NSLog(@"[JailbreakDetector] WRITE TEST SUCCESS");
                    flags |= JB_WRITE_TEST;
                }
            }
            fclose(f);
            remove(testPath);
        } else {
            NSLog(@"[JailbreakDetector] fopen(%s) failed errno=%d (%s)", testPath, errno, strerror(errno));
        }

        // 3) access test
        const char *accessPaths[] = { 
	    "/var/lib/apt",      // apt tồn tại trên máy jailbreak
            "/Library/MobileSubstrate", // tweak engine
            "/usr/sbin/sshd",    // SSH daemon
            NULL  };
        for (int i = 0; accessPaths[i] != NULL; i++) {
            if (access(accessPaths[i], X_OK) == 0) {
                NSLog(@"[JailbreakDetector] ACCESS OK: %s", accessPaths[i]);
                flags |= JB_ACCESS_TEST;
            }
        }

        // 4) dyld images
        uint32_t count = _dyld_image_count();
        for (uint32_t i = 0; i < count; i++) {
            const char* name = _dyld_get_image_name(i);
            if (name) {
                if (strstr(name, "MobileSubstrate") || strstr(name, "Substrate") ||
                    strstr(name, "Tweak") || strstr(name, "cyinject") ||
                    strstr(name, "libsubstitute") || strstr(name, "Frida")) {
                    NSLog(@"[JailbreakDetector] DYLD IMAGE: %s", name);
                    flags |= JB_DYLD_TEST;
                }
            }
        }

        // 5) /User/Applications
        struct stat stUserApps;
        if (stat("/User/Applications/", &stUserApps) == 0) {
            NSLog(@"[JailbreakDetector] /User/Applications exists");
            flags |= JB_USER_APPS;
        }
    }
    @catch (NSException *exception) {
        NSLog(@"[JailbreakDetector] Exception: %@", exception);
        flags |= JB_EXCEPTION;
    }

    if (flags != JB_NONE) {
        NSLog(@"[JailbreakDetector] Detected jailbreak flags: %d", flags);
    } else {
        NSLog(@"[JailbreakDetector] No jailbreak indicators found.");
    }
    return flags;
}
