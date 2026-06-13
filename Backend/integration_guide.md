# Integration Guide: Proctor Vision API

This guide provides everything you need to link the Python AI Proctoring Service (Hugging Face / FastAPI) with your **.NET Backend** and your **Frontend Application**.

---

## Architecture Overview

1. **Backend (.NET)** generates a secure JWT token for the student.
2. **Frontend** receives the token and opens a WebSocket to the **AI Proctoring API**, streaming webcam frames.
3. **AI Proctoring API** analyzes the frames using YOLO.
4. If cheating is detected, the **AI Proctoring API** sends a signed HTTP Webhook back to the **Backend (.NET)**.

---

## 1. Backend (.NET) Integration

Your .NET backend has two responsibilities: issuing JWT tokens to the frontend and receiving secure webhooks from the AI.

### A. Issuing the JWT Token
The AI server uses the exact same JWT configuration as your backend. You don't need a special token; the student's standard authentication token will work, provided it contains the `Id` claim.

**Required Token Settings:**
- **Key:** `Fgw7EqKJWhE0yPtRYOFarFmbUfFP5pej`
- **Issuer:** `Neura`
- **Audience:** `NeuraClient`
- **Algorithm:** `HS256`
- **Required Claims:** `Id` (The student's unique identifier).

### B. Receiving the Cheating Webhook
When the AI detects cheating, it makes an HTTP POST request to your backend.

**Endpoint Setup in .NET:**
```csharp
[ApiController]
[Route("api/webhooks")]
public class ProctorWebhookController : ControllerBase
{
    private readonly string _hmacSecret = "YOUR_GENERATED_WEBHOOK_SECRET";

    [HttpPost("cheating_alert")]
    public async Task<IActionResult> ReceiveAlert()
    {
        // 1. Read raw body bytes for HMAC verification
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes(rawBody);

        // 2. Validate the HMAC-SHA256 signature
        var signatureHeader = Request.Headers["X-Signature-SHA256"].ToString();
        if (string.IsNullOrEmpty(signatureHeader) || !signatureHeader.StartsWith("sha256="))
        {
            return Unauthorized("Missing or invalid signature header.");
        }

        var providedSignature = signatureHeader.Substring(7); // Remove "sha256="
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_hmacSecret));
        var computedSignature = BitConverter.ToString(hmac.ComputeHash(bodyBytes)).Replace("-", "").ToLower();

        if (providedSignature != computedSignature)
        {
            return Unauthorized("Signature mismatch.");
        }

        // 3. Deserialize the payload
        var alert = System.Text.Json.JsonSerializer.Deserialize<CheatingAlertPayload>(rawBody);

        // 4. Decode the suspicious frame (Base64 -> byte[])
        if (!string.IsNullOrEmpty(alert.frameData))
        {
            byte[] imageBytes = Convert.FromBase64String(alert.frameData);
            // Example: System.IO.File.WriteAllBytes($"suspicious_frame_{alert.examId}_{alert.timestamp}.jpg", imageBytes);
            // Or upload to Azure Blob Storage / AWS S3
        }

        // 5. Save to database (e.g., flag the student's exam attempt)
        // dbContext.Alerts.Add(...);
        
        return Ok();
    }
}

public class CheatingAlertPayload
{
    public string examId { get; set; }
    public string studentId { get; set; }
    public double timestamp { get; set; }
    public string severity { get; set; }  // "CRITICAL" or "MAJOR"
    public string reason { get; set; }
    public string frameData { get; set; } // Base64 encoded JPEG frame
}
```

---

## 2. Frontend Integration

Your frontend needs to access the user's webcam, convert frames to JPEGs, and stream them over a WebSocket connection.

### A. Connection Setup
The WebSocket URL format is:
`wss://<YOUR_HF_SPACE_URL>/ws/proctor/{examId}?token={jwtToken}`

### B. Example Frontend Implementation (Vanilla / React concept)

```javascript
// 1. Configuration
const WS_HOST = 'wss://neura-lms-proctor-vision-api.hf.space'; // Replace with your host
const examId = 'exam_101'; // Get from your app state/routing
const jwtToken = 'eyJhb...'; // Get from your auth context

const wsUrl = `${WS_HOST}/ws/proctor/${encodeURIComponent(examId)}?token=${encodeURIComponent(jwtToken)}`;

// 2. State
let ws = null;
let frameInterval = null;
const canvas = document.createElement('canvas');
canvas.width = 640;
canvas.height = 480;
const ctx = canvas.getContext('2d');

// 3. Connect to WebSocket
function connectProctoring(videoElement) {
    ws = new WebSocket(wsUrl);
    ws.binaryType = 'blob';

    ws.onopen = () => {
        console.log('Proctoring connected');
        // Start sending frames at 2 FPS (every 500ms)
        frameInterval = setInterval(() => sendFrame(videoElement), 500);
    };

    ws.onmessage = (event) => {
        const response = JSON.parse(event.data);
        if (response.active_alerts && response.active_alerts.length > 0) {
            console.warn("AI Alert:", response.active_alerts);
            // Optionally show a warning to the user on screen
        }
    };

    ws.onclose = (event) => {
        clearInterval(frameInterval);
        console.log(`Proctoring disconnected. Code: ${event.code}`);
        
        // Handle Permanent Rejections
        const PERMANENT_ERRORS = new Set([4001, 4002, 4003, 4004]);
        if (!PERMANENT_ERRORS.has(event.code)) {
            // Implement Exponential Backoff Reconnection here
            setTimeout(() => connectProctoring(videoElement), 2000); 
        } else {
            alert("Proctoring session ended or rejected.");
        }
    };
}

// 4. Send Frame function
function sendFrame(videoElement) {
    if (ws.readyState !== WebSocket.OPEN) return;
    
    // Draw current video frame to canvas
    ctx.drawImage(videoElement, 0, 0, canvas.width, canvas.height);
    
    // Compress to JPEG and send as Blob
    canvas.toBlob((blob) => {
        if (ws.readyState === WebSocket.OPEN) {
            ws.send(blob);
        }
    }, 'image/jpeg', 0.5); // 0.5 quality keeps size small (~30-50kb)
}

// 5. Start Webcam
navigator.mediaDevices.getUserMedia({ video: true })
    .then((stream) => {
        const videoElement = document.getElementById('student-video');
        videoElement.srcObject = stream;
        
        // Connect to AI only after video is playing
        videoElement.onplay = () => connectProctoring(videoElement);
    })
    .catch(err => console.error("Webcam access denied", err));
```

---

## 3. Deployment Checklist

Before going live, ensure you configure the environment variables correctly on Hugging Face (or your deployment server).

### Environment Variables for the AI Server
| Variable | Value | Description |
|----------|-------|-------------|
| `JWT_SECRET` | `Fgw7EqKJWhE0...` | Must match your .NET appsettings exactly. |
| `WEBHOOK_HMAC_SECRET` | *(Generated Hex)* | Must match the `_hmacSecret` in your .NET webhook controller. |
| `WEBHOOK_URL` | `https://your-net-server.com/api/webhooks/cheating_alert` | Where the AI sends alerts. |
| `ALLOWED_ORIGINS` | `https://your-frontend-domain.com` | Prevents unauthorized domains from using the API. |

### System Limits to Keep in Mind
- **Max FPS:** The server strictly rate-limits at **2 frames per second**. Sending frames faster will just waste client bandwidth (the server drops them).
- **Max Frame Size:** Frames over **512 KB** are dropped. A 640x480 JPEG at 50% quality is usually ~30-50 KB, which is perfect.
- **Cooldowns:** If a student looks away, the webhook fires. If they *keep* looking away, the webhook will NOT spam the backend. It waits **30 seconds** before sending another alert of the same type.
