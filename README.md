# AppwriteHelper

Library for using Appwrite in C# projects.

# Notice

This package is under active development and should be used with caution, as significant changes may occur.

## Installation NuGet:
PM > Install-Package AppwriteHelper

# Usage:

# Compatibility

This package follows the Appwrite .NET SDK versioning scheme (x.x.0) and is compatible with the corresponding Appwrite .NET SDK release. A drawback of following Appwrite's versioning is that breaking changes with the server are not immediately obvious when using semantic versioning. The patch version number (0.0.x) does not follow the SDK's versioning and may differ.

# Important Notes

## Session Validation

**Warning:** When an endpoint is authorized with AppwriteHelper Session authentication, the system cannot automatically detect if the session has been revoked on the Appwrite server unless the endpoint itself makes an Appwrite API call using the session from the AppwriteHelper cookie.

To mitigate this limitation:			
- **Use short cookie expiration times** to minimize the window of vulnerability
- **Make an Appwrite server call at the beginning of your endpoint** to validate the session is still active		

Without one of these measures, a revoked session cookie may still grant access until it expires locally.


