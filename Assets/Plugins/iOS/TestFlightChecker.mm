#import <Foundation/Foundation.h>

extern "C"
{
    bool _IsTestFlight()
    {
        @autoreleasepool
        {
            NSBundle *bundle = [NSBundle mainBundle];
            NSURL *receiptURL = [bundle appStoreReceiptURL];
            
            if (!receiptURL)
                return false;
                
            NSString *receiptFileName = [receiptURL lastPathComponent];
            
            // TestFlight/sanbox → file tên là "sandboxReceipt"
            // App Store → file tên là "receipt"
            return [receiptFileName isEqualToString:@"sandboxReceipt"];
        }
    }
}