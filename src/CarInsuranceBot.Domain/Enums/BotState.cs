namespace CarInsuranceBot.Domain.Enums;

//A list of all possible states of the dialogue with the user.
public enum BotState 
{
    Started,
    WaitingForPassport,
    WaitingForVehicleDoc,
    ConfirmingPassportData,
    ConfirmingVehicleData,
    WaitingForPriceConfirmation,
    PolisyIssued
}