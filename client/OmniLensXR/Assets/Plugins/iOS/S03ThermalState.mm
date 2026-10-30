#import <Foundation/Foundation.h>

extern "C" int OmniLensS03ThermalState(void)
{
    if (@available(iOS 11.0, *))
    {
        return (int)[[NSProcessInfo processInfo] thermalState];
    }

    return -1;
}
