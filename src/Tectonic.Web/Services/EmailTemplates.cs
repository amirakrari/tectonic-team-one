using Tectonic.Web.Models;

namespace Tectonic.Web.Services;

public record EmailSample(string Subject, string Body, string Cta);

// Example emails for the preview on Appearance > Email reminders: per language, tone and sample rule.
// Wording is a proposal for the API team, who write the real emails.
public static class EmailTemplates
{
    public static readonly IReadOnlyList<string> Tones = ["Playful", "Informal", "Formal", "Serious", "Panicky"];
    public static readonly IReadOnlyList<string> Designs = ["Classic", "Minimal", "Theme"];
    public static readonly IReadOnlyList<string> TextSizes = ["Small", "Regular", "Large"];

    public static readonly IReadOnlyList<NotificationType> Samples =
        [NotificationType.PriceIncrease, NotificationType.PriceDecrease, NotificationType.PaymentIncomplete];

    public static string Emoji(string tone, NotificationType type) => tone switch
    {
        "Playful" => type == NotificationType.PriceDecrease ? "🎉" : "👀",
        "Informal" => "👋",
        "Panicky" => "🚨",
        _ => type switch
        {
            NotificationType.PriceIncrease => "📈",
            NotificationType.PriceDecrease => "📉",
            _ => "⏳",
        },
    };

    public static EmailSample Get(Lang lang, string tone, NotificationType type) =>
        All.TryGetValue((lang, tone, type), out var sample) ? sample : All[(Lang.En, "Formal", type)];

    private const NotificationType Up = NotificationType.PriceIncrease;
    private const NotificationType Down = NotificationType.PriceDecrease;
    private const NotificationType Part = NotificationType.PaymentIncomplete;

    private static readonly Dictionary<(Lang, string, NotificationType), EmailSample> All = new()
    {
        // ---------- English ----------
        [(Lang.En, "Playful", Up)] = new("Psst… Netflix got pricier",
            "Heads up, John! Netflix just bumped your bill from €13.99 to €15.99. Still binge-worthy? Your call.", "Take a look"),
        [(Lang.En, "Informal", Up)] = new("Netflix went up",
            "Hi John, just so you know: Netflix now costs €15.99 instead of €13.99.", "See details"),
        [(Lang.En, "Formal", Up)] = new("Price change: Netflix",
            "Dear John, we noticed that your payment to Netflix increased from €13.99 to €15.99. You can review this expense in your KBC Brussels app.", "View in the app"),
        [(Lang.En, "Serious", Up)] = new("Netflix price increased by €2.00",
            "John, your Netflix payment rose from €13.99 to €15.99 (+14%). That is €24.00 more per year. Review whether this subscription is still worth it.", "Review expense"),
        [(Lang.En, "Panicky", Up)] = new("Netflix just got MORE expensive!",
            "John!! Netflix jumped from €13.99 to €15.99! That's €24 extra every year! Check it right now!", "Check it now"),

        [(Lang.En, "Playful", Down)] = new("Good news: Spotify got cheaper",
            "Nice one, John! Spotify dropped from €10.99 to €8.99. More money for snacks.", "Take a look"),
        [(Lang.En, "Informal", Down)] = new("Spotify is cheaper now",
            "Hi John, Spotify now costs €8.99 instead of €10.99. Enjoy the savings!", "See details"),
        [(Lang.En, "Formal", Down)] = new("Price change: Spotify",
            "Dear John, your payment to Spotify decreased from €10.99 to €8.99. No action is needed.", "View in the app"),
        [(Lang.En, "Serious", Down)] = new("Spotify price decreased by €2.00",
            "John, your Spotify payment went down from €10.99 to €8.99 (−18%). Check that your plan hasn't changed.", "Review expense"),
        [(Lang.En, "Panicky", Down)] = new("Wait, Spotify got CHEAPER?!",
            "John, Spotify suddenly dropped from €10.99 to €8.99! Make sure nothing changed in your plan!", "Check it now"),

        [(Lang.En, "Playful", Part)] = new("Almost there! €100 to go",
            "Hey John, you've paid €400 of your €500 Engie bill. Just €100 left before 15 October. You've got this!", "Pay the rest"),
        [(Lang.En, "Informal", Part)] = new("€100 left on your Engie bill",
            "Hi John, you still need to pay €100 of your Engie bill (€500) before 15 October.", "Pay now"),
        [(Lang.En, "Formal", Part)] = new("Reminder: outstanding balance for Engie",
            "Dear John, an amount of €100 remains outstanding on your Engie bill of €500. Please complete the payment before 15 October.", "Pay in the app"),
        [(Lang.En, "Serious", Part)] = new("Outstanding: €100 on your Engie bill",
            "John, €100 of your €500 Engie bill is still unpaid. Due date: 15 October. Late payment may lead to extra costs.", "Pay now"),
        [(Lang.En, "Panicky", Part)] = new("Don't forget: €100 still UNPAID!",
            "John! You still owe €100 on your Engie bill and it's due 15 October! Pay it now to avoid trouble!", "Pay it NOW"),

        // ---------- Français ----------
        [(Lang.Fr, "Playful", Up)] = new("Psst… Netflix est devenu plus cher",
            "Attention John ! Netflix est passé de 13,99 € à 15,99 €. Toujours aussi indispensable ? À toi de voir.", "Jeter un œil"),
        [(Lang.Fr, "Informal", Up)] = new("Netflix a augmenté",
            "Salut John, pour info : Netflix coûte maintenant 15,99 € au lieu de 13,99 €.", "Voir le détail"),
        [(Lang.Fr, "Formal", Up)] = new("Changement de prix : Netflix",
            "Cher John, nous avons constaté que votre paiement à Netflix est passé de 13,99 € à 15,99 €. Vous pouvez consulter cette dépense dans votre app KBC Brussels.", "Voir dans l'app"),
        [(Lang.Fr, "Serious", Up)] = new("Prix Netflix en hausse de 2,00 €",
            "John, votre paiement à Netflix est passé de 13,99 € à 15,99 € (+14 %). Soit 24,00 € de plus par an. Vérifiez si cet abonnement en vaut toujours la peine.", "Vérifier la dépense"),
        [(Lang.Fr, "Panicky", Up)] = new("Netflix est devenu PLUS CHER !",
            "John !! Netflix est passé de 13,99 € à 15,99 € ! Ça fait 24 € de plus chaque année ! Vérifiez tout de suite !", "Vérifier maintenant"),

        [(Lang.Fr, "Playful", Down)] = new("Bonne nouvelle : Spotify est moins cher",
            "Bien joué, John ! Spotify est passé de 10,99 € à 8,99 €. Plus d'argent pour les snacks.", "Jeter un œil"),
        [(Lang.Fr, "Informal", Down)] = new("Spotify est moins cher",
            "Salut John, Spotify coûte maintenant 8,99 € au lieu de 10,99 €. Profites-en !", "Voir le détail"),
        [(Lang.Fr, "Formal", Down)] = new("Changement de prix : Spotify",
            "Cher John, votre paiement à Spotify est passé de 10,99 € à 8,99 €. Aucune action n'est requise.", "Voir dans l'app"),
        [(Lang.Fr, "Serious", Down)] = new("Prix Spotify en baisse de 2,00 €",
            "John, votre paiement à Spotify est passé de 10,99 € à 8,99 € (−18 %). Vérifiez que votre formule n'a pas changé.", "Vérifier la dépense"),
        [(Lang.Fr, "Panicky", Down)] = new("Attendez, Spotify est MOINS CHER ?!",
            "John, Spotify est soudain passé de 10,99 € à 8,99 € ! Vérifiez que rien n'a changé dans votre formule !", "Vérifier maintenant"),

        [(Lang.Fr, "Playful", Part)] = new("Presque fini ! Plus que 100 €",
            "Hé John, tu as payé 400 € de ta facture Engie de 500 €. Plus que 100 € avant le 15 octobre. Tu y es presque !", "Payer le reste"),
        [(Lang.Fr, "Informal", Part)] = new("Il reste 100 € sur ta facture Engie",
            "Salut John, il te reste 100 € à payer sur ta facture Engie (500 €) avant le 15 octobre.", "Payer maintenant"),
        [(Lang.Fr, "Formal", Part)] = new("Rappel : solde restant dû pour Engie",
            "Cher John, un montant de 100 € reste dû sur votre facture Engie de 500 €. Merci de compléter le paiement avant le 15 octobre.", "Payer dans l'app"),
        [(Lang.Fr, "Serious", Part)] = new("Impayé : 100 € sur votre facture Engie",
            "John, 100 € de votre facture Engie de 500 € restent impayés. Échéance : 15 octobre. Un retard de paiement peut entraîner des frais supplémentaires.", "Payer maintenant"),
        [(Lang.Fr, "Panicky", Part)] = new("N'oubliez pas : 100 € toujours IMPAYÉS !",
            "John ! Vous devez encore 100 € sur votre facture Engie et l'échéance est le 15 octobre ! Payez maintenant pour éviter les ennuis !", "Payer MAINTENANT"),

        // ---------- Nederlands ----------
        [(Lang.Nl, "Playful", Up)] = new("Psst… Netflix is duurder geworden",
            "Let op, John! Netflix ging van € 13,99 naar € 15,99. Nog steeds de moeite waard? Jij beslist.", "Even kijken"),
        [(Lang.Nl, "Informal", Up)] = new("Netflix is duurder",
            "Hoi John, even ter info: Netflix kost nu € 15,99 in plaats van € 13,99.", "Bekijk details"),
        [(Lang.Nl, "Formal", Up)] = new("Prijswijziging: Netflix",
            "Beste John, we merkten dat je betaling aan Netflix is gestegen van € 13,99 naar € 15,99. Je kunt deze uitgave bekijken in je KBC Brussels-app.", "Bekijk in de app"),
        [(Lang.Nl, "Serious", Up)] = new("Prijs Netflix € 2,00 hoger",
            "John, je betaling aan Netflix steeg van € 13,99 naar € 15,99 (+14%). Dat is € 24,00 meer per jaar. Bekijk of dit abonnement het nog waard is.", "Uitgave bekijken"),
        [(Lang.Nl, "Panicky", Up)] = new("Netflix is net DUURDER geworden!",
            "John!! Netflix sprong van € 13,99 naar € 15,99! Dat is € 24 extra per jaar! Check het meteen!", "Nu checken"),

        [(Lang.Nl, "Playful", Down)] = new("Goed nieuws: Spotify is goedkoper",
            "Top, John! Spotify zakte van € 10,99 naar € 8,99. Meer geld voor snacks.", "Even kijken"),
        [(Lang.Nl, "Informal", Down)] = new("Spotify is nu goedkoper",
            "Hoi John, Spotify kost nu € 8,99 in plaats van € 10,99. Geniet ervan!", "Bekijk details"),
        [(Lang.Nl, "Formal", Down)] = new("Prijswijziging: Spotify",
            "Beste John, je betaling aan Spotify is gedaald van € 10,99 naar € 8,99. Je hoeft niets te doen.", "Bekijk in de app"),
        [(Lang.Nl, "Serious", Down)] = new("Prijs Spotify € 2,00 lager",
            "John, je betaling aan Spotify daalde van € 10,99 naar € 8,99 (−18%). Controleer of je abonnement niet is gewijzigd.", "Uitgave bekijken"),
        [(Lang.Nl, "Panicky", Down)] = new("Wacht, Spotify is GOEDKOPER?!",
            "John, Spotify zakte plots van € 10,99 naar € 8,99! Controleer of er niets aan je abonnement is veranderd!", "Nu checken"),

        [(Lang.Nl, "Playful", Part)] = new("Bijna klaar! Nog € 100 te gaan",
            "Hé John, je hebt € 400 van je Engie-factuur van € 500 betaald. Nog maar € 100 vóór 15 oktober. Je bent er bijna!", "Rest betalen"),
        [(Lang.Nl, "Informal", Part)] = new("Nog € 100 open op je Engie-factuur",
            "Hoi John, je moet nog € 100 van je Engie-factuur (€ 500) betalen vóór 15 oktober.", "Nu betalen"),
        [(Lang.Nl, "Formal", Part)] = new("Herinnering: openstaand saldo voor Engie",
            "Beste John, er staat nog € 100 open op je Engie-factuur van € 500. Gelieve de betaling vóór 15 oktober te voltooien.", "Betalen in de app"),
        [(Lang.Nl, "Serious", Part)] = new("Openstaand: € 100 op je Engie-factuur",
            "John, € 100 van je Engie-factuur van € 500 is nog niet betaald. Vervaldag: 15 oktober. Laattijdige betaling kan extra kosten meebrengen.", "Nu betalen"),
        [(Lang.Nl, "Panicky", Part)] = new("Niet vergeten: € 100 nog NIET BETAALD!",
            "John! Je moet nog € 100 betalen op je Engie-factuur en die vervalt op 15 oktober! Betaal nu om problemen te vermijden!", "Betaal NU"),
    };
}
