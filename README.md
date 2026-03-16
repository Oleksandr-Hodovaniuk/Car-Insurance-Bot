## Car Insurance Telegram Bot

This project is an ASP.NET Core 10.0 application that exposes a Telegram webhook endpoint and guides users through purchasing a car insurance policy.  
The bot uses Groq (OpenAI‑compatible API) for conversational messages and policy text generation, and a (mocked) Mindee service for document OCR.

### Architecture Overview

- **API layer (`CarInsuranceBot`)**: ASP.NET Core Web API that exposes the `/webhook` endpoint for Telegram updates and configures DI and JSON options.
- **Application layer (`CarInsuranceBot.Application`)**: Contains the state machine (`BotDispatcher`), user session management, handlers for every step, and policy generation logic.
- **Infrastructure layer (`CarInsuranceBot.Infrastructure`)**: Integrations with external services:
  - `GroqService` (`IAiService`) – calls Groq/OpenAI‑compatible chat API.
  - `MockMindeeService` (`IMindeeService`) – mock OCR for passport and vehicle documents.
- **Domain layer (`CarInsuranceBot.Domain`)**: Holds core models (`UserSession`, `InsurancePolicy`, `ExtractedDocumentData`) and enums (`BotState`).

---

### Setup and Dependencies

#### Prerequisites

- **.NET SDK 10.0** (matching the `TargetFramework` in the `.csproj` files).
- **Telegram bot token** created via BotFather.
- **Groq/OpenAI‑compatible** API access (URL, API key, model name).
- Optionally: Mindee account if you replace the mock OCR with a real implementation.

#### Project Structure (short)

- `src/CarInsuranceBot` – Web API (entry point, controllers, configuration).
- `src/CarInsuranceBot.Application` – handlers, state machine, services, interfaces.
- `src/CarInsuranceBot.Infrastructure` – external integrations (`GroqService`, `MockMindeeService`).
- `src/CarInsuranceBot.Domain` – entities, enums, exceptions.

#### Environment Variables / .env.example

The app loads configuration from an `.env` file in the solution root (see `Program.cs` with `DotNetEnv.Env.Load("../../.env")`) and from environment variables.  
To avoid committing secrets, this repository should contain a **template** file named `.env.example` that lists all required keys with dummy values.  
Developers then create their own `.env` by copying this file:

```bash
cp .env.example .env
```

In `.env.example`, include at least the following keys:

- **Telegram**
  - `BotSettings__Token` – Telegram bot token.
  - `BotSettings__WebhookSecretToken` – secret token used to validate Telegram webhook requests via `X-Telegram-Bot-Api-Secret-Token` header.
- **Groq / OpenAI‑compatible**
  - `GroqAiSettings__URL` – base URL (endpoint) of the Groq/OpenAI‑compatible API.
  - `GroqAiSettings__ApiKey` – API key for Groq.
  - `GroqAiSettings__Model` – model name (e.g. `llama-3.1-8b-instant` or similar).


#### Installing Dependencies

From the solution root:

```bash
dotnet restore
```

This restores all NuGet packages, including:

- `Telegram.Bot` – Telegram Bot API client.
- `DotNetEnv` – load `.env` file.
- `OpenAI` – client for OpenAI‑compatible / Groq API.
- `QuestPDF` – PDF generation for the final insurance policy.
- `Mindee` – SDK for Mindee OCR (currently mocked by `MockMindeeService`).
- `Microsoft.Extensions.*` – caching and DI abstractions.

#### Running the Bot Locally

From the solution root:

```bash
dotnet run --project src/CarInsuranceBot/CarInsuranceBot.csproj
```

The API will start and expose controllers mapped in `Program.cs`. The main endpoint of interest is:

- `POST /webhook` – receives Telegram `Update` objects and passes them to `BotDispatcher`.

To test end‑to‑end with Telegram you must configure the bot’s webhook URL (see below).

---

### Telegram Webhook Configuration

The `BotController` is defined as:

- Route: `/webhook`
- Method: `POST`
- Body: Telegram `Update` JSON
- Security: validates the `X-Telegram-Bot-Api-Secret-Token` header against `BotSettings__WebhookSecretToken`.

To connect Telegram with your running API:

1. **Deploy** the API to a public URL with HTTPS (e.g. `https://your-domain.com`).
2. **Set the webhook** for your bot (replace placeholders):

```bash
curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook" \
  -d "url=https://your-domain.com/webhook" \
  -d "secret_token=super-secret-webhook-token"
```

3. Telegram will start sending updates (messages, photos, etc.) to `POST https://your-domain.com/webhook` with the header `X-Telegram-Bot-Api-Secret-Token`.

---

### Detailed Bot Workflow

The core of the dialogue logic is implemented in:

- `BotDispatcher` – routes each incoming `Update` to the appropriate handler based on `UserSession.State` (`BotState`).
- Handlers:
  - `StartHandler` – `/start` greeting and first instruction.
  - `DocumentHandler` – passport and vehicle document processing via `IMindeeService`.
  - `ConfirmHandler` – confirmation of OCR‑extracted fields.
  - `PaymentHandler` – price acceptance flow (fixed price of 100 USD).
  - `PolicyHandler` – generation and sending of the final PDF policy.

#### 1. Starting the Conversation (`/start`)

- User sends `/start` to the Telegram bot.
- `BotDispatcher`:
  - Clears any existing session for that `chatId`.
  - Creates a new `UserSession` with `State = Started`.
  - Calls `StartHandler.HandleAsync`.
- `StartHandler`:
  - Uses `IAiService.GetResponseAsync` with a system prompt instructing the AI to:
    - Greet the user.
    - Explain that this is a car insurance assistant.
    - Ask the user to send a photo of their **passport**.
    - Always respond in **English**.
  - Sends AI response via `ITelegramBotClient.SendMessage`.
  - Sets session state to `BotState.WaitingForPassport`.

#### 2. Uploading Passport Photo

- In state `WaitingForPassport`, any incoming message is handled by:
  - `DocumentHandler.HandlePassportAsync` → `HandleDocumentAsync(isPassport: true)`.
- `DocumentHandler`:
  - If the message does **not** contain a photo:
    - Calls AI to politely remind the user to send a passport photo.
  - If a photo is present:
    - Downloads it from Telegram (`GetInfoAndDownloadFile`).
    - Sends the stream to `IMindeeService.ExtractPassportDataAsync`.
    - On success:
      - Saves the extracted fields into `session.PassportData`.
      - Sends a human‑readable list of extracted fields to the user.
      - Uses AI to generate a short message asking the user to confirm correctness (Yes/No).
      - Sets state to `BotState.ConfirmingPassportData`.
    - On OCR failure (`DocumentParseException`):
      - Uses AI to apologize and ask the user to retake the photo (better lighting, full document visible).

#### 3. Confirming Passport Data

- In state `ConfirmingPassportData`, incoming messages go to:
  - `ConfirmHandler.HandleConfirmAsync(isPassport: true)`.
- `ConfirmHandler`:
  - Interprets the text as `"yes"` or not.
  - If **Yes**:
    - Marks `session.PassportData.IsConfirmed = true`.
    - Sets state to `BotState.WaitingForVehicleDoc`.
    - Asks (via AI) the user to send a **vehicle registration document** photo.
  - If **No**:
    - Resets state back to `BotState.WaitingForPassport`.
    - Uses AI to apologize and ask the user to retake the passport photo.

#### 4. Uploading Vehicle Document Photo

- In state `WaitingForVehicleDoc`, incoming messages are handled by:
  - `DocumentHandler.HandleVehicleDocAync` → `HandleDocumentAsync(isPassport: false)`.
- Similar logic as for the passport:
  - If no photo → AI reminder to send a vehicle registration document photo.
  - If photo:
    - Download, send to `IMindeeService.ExtractVehicleDocDataAsync`.
    - Save as `session.VehicleData`.
    - Show extracted fields.
    - Ask user to confirm via AI.
    - Set state to `BotState.ConfirmingVehicleData`.

#### 5. Confirming Vehicle Data

- In state `ConfirmingVehicleData`, incoming messages go to:
  - `ConfirmHandler.HandleConfirmAsync(isPassport: false)`.
- If **Yes**:
  - Mark `session.VehicleData.IsConfirmed = true`.
  - Set state to `BotState.WaitingForPriceConfirmation`.
  - Use AI to:
    - Inform the user that the insurance price is **100 USD**.
    - Ask if they agree to proceed (`Yes` / `No`).
- If **No**:
  - State returns to `BotState.WaitingForVehicleDoc`.
  - AI asks the user to retake the vehicle document photo.

#### 6. Price Confirmation and Payment Step

- In state `WaitingForPriceConfirmation`, incoming messages go to `PaymentHandler.HandleAsync`.
- `PaymentHandler`:
  - Normalizes message text and checks if it is `"yes"`.
  - If **Yes**:
    - Sets state to `BotState.PolisyIssued`.
    - Sends a summary:
      - Passport details.
      - Vehicle details.
      - Fixed price: `100 USD`.
    - Uses AI to tell the user that their PDF policy is being generated.
    - Calls `PolicyHandler.HandleAsync` to generate and send the PDF.
  - If **No** or anything else:
    - Uses AI to explain that the price is fixed at **100 USD** and asks again if the user wants to proceed (`Yes` / `No`).

#### 7. Policy Generation and Delivery

- `PolicyHandler.HandleAsync`:
  - Calls `IPolicyService.GenerateAsync(session)` to create an `InsurancePolicy`.
  - `PolicyService`:
    - Builds `passportInfo` and `vehicleInfo` from confirmed session fields.
    - Calls `IAiService.GeneratePolicyTextAsync` (Groq) to create a formal policy text.
    - Generates a PDF from that text using **QuestPDF** (A4, header/footer, issue date, validity, price).
    - Returns an `InsurancePolicy` containing:
      - `PolicyText`.
      - `PdfBytes`.
  - `PolicyHandler` sends the PDF as a Telegram document with a caption including:
    - Policy number.
    - Valid until date.
    - Price and currency.
  - The user session is removed (`ISessionService.Remove`), completing the dialog.

#### 8. Error Handling

- `BotDispatcher` wraps each update processing in a try/catch:
  - On any unhandled exception:
    - Logs the error to console.
    - Attempts to send a generic apology message and instructs the user to type `/start` again.
- Individual services (like `GroqService`, `MockMindeeService`) also log and throw specific exceptions where appropriate.

---

### Example Interaction Flows

Below are simplified, **ideal‑path** examples (in English) to help understand how the bot behaves.

#### Flow 1: Successful Policy Purchase

1. **User**: `/start`  
   **Bot**: Greets the user, explains it can help purchase car insurance, asks to send a **passport photo**.
2. **User**: sends passport photo.  
   **Bot**:
   - Shows extracted passport fields (name, DOB, nationality, document number, etc.).
   - Asks: “Please reply ‘Yes’ if the data is correct or ‘No’ if it is incorrect.”
3. **User**: `Yes`  
   **Bot**: “Great, now please send a photo of your **vehicle registration document**.”
4. **User**: sends vehicle document photo.  
   **Bot**:
   - Shows extracted vehicle data (owner, license plate, model/year, etc.).
   - Asks the user to confirm (Yes/No).
5. **User**: `Yes`  
   **Bot**: “Your insurance price is **100 USD**. Do you agree to proceed with the purchase? Reply Yes or No.”
6. **User**: `Yes`  
   **Bot**:
   - Sends a summary of passport and vehicle details and the fixed price `100 USD`.
   - Informs that the PDF policy is being generated.
   - Sends the **PDF policy document** as a Telegram file with policy number, validity period and price.

#### Flow 2: Incorrect OCR Data for Passport

1. **User**: `/start` → bot asks for passport photo.
2. **User**: sends passport photo.
3. **Bot**: shows extracted fields and asks for confirmation.
4. **User**: `No`  
   **Bot**:
   - Apologizes for the incorrect data.
   - Asks the user to retake the passport photo with good lighting and full visibility.
   - State goes back to `WaitingForPassport`.

#### Flow 3: User Disagrees with Price

1. Up to vehicle data confirmation – same as in Flow 1.
2. **Bot**: “The insurance price is 100 USD. Do you agree to proceed? Reply Yes or No.”
3. **User**: `No`  
   **Bot**:
   - Politely explains that **100 USD is the only available price option**.
   - Asks again if the user wants to proceed by replying `Yes` or `No`.

---

### Session Management

- `SessionService` stores `UserSession` instances in an in‑memory cache (`IMemoryCache`).
- Each session:
  - Is keyed by `chatId`.
  - Has a sliding expiration of **1 hour** (`SessionTtl`).
  - Tracks:
    - Current `BotState`.
    - Extracted and (optionally) confirmed `PassportData` and `VehicleData`.
    - Last updated timestamp `UpdatedAt`.
- Sessions are explicitly removed when the policy is successfully issued.

---

### Link to the Telegram Bot

Add your public bot link here once deployed:

- **Telegram bot**: `https://t.me/your_car_insurance_bot`  <!-- TODO: replace with actual bot link -->

---

### Notes and Possible Extensions

- Replace `MockMindeeService` with a real `Mindee` implementation for production OCR.
- Add persistence (e.g. database) for issued policies and audit logs.
- Localize prompts and flows to other languages (currently prompts explicitly enforce English responses from AI).
- Implement richer pricing logic (instead of fixed 100 USD) based on extracted vehicle and driver data.

