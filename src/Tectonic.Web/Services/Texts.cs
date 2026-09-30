using System.Globalization;

namespace Tectonic.Web.Services;

public enum Lang { En, Fr, Nl }

public record T(string En, string Fr, string Nl);

// Every UI string in English, French and Dutch. Keys are "area.name". A missing key renders as the key
// itself, so gaps are easy to spot. {0}, {1}… are string.Format placeholders.
public static class Texts
{
    public static readonly IReadOnlyDictionary<string, T> All = new Dictionary<string, T>
    {
        // Common
        ["common.back"] = new("Back", "Retour", "Terug"),
        ["common.accountMenu"] = new("Account menu", "Menu du compte", "Accountmenu"),
        ["common.edit"] = new("Edit", "Modifier", "Wijzigen"),
        ["common.cancel"] = new("Cancel", "Annuler", "Annuleren"),
        ["common.save"] = new("Save", "Enregistrer", "Opslaan"),
        ["common.turnedOn"] = new("{0} turned on", "{0} activé", "{0} ingeschakeld"),
        ["common.turnedOff"] = new("{0} turned off", "{0} désactivé", "{0} uitgeschakeld"),

        // Left rail (KBC Brussels sections)
        ["nav.personal"] = new("Personal", "Privé", "Privé"),
        ["nav.business"] = new("Business", "Pro", "Zakelijk"),
        ["nav.switchAria"] = new("Personal or business", "Privé ou Pro", "Privé of zakelijk"),
        ["nav.payments"] = new("Payments", "Paiements", "Betalen"),
        ["nav.savings"] = new("Savings & Investments", "Épargne & Placements", "Sparen & Beleggen"),
        ["nav.home"] = new("Home", "Logement", "Wonen"),
        ["nav.family"] = new("Family", "Famille", "Gezin"),
        ["nav.mobility"] = new("Mobility", "Mobilité", "Mobiliteit"),
        ["nav.url.savings"] = new("https://www.kbcbrussels.be/retail/en/savings.html",
            "https://www.kbcbrussels.be/particuliers/fr/epargner.html",
            "https://www.kbcbrussels.be/particulieren/nl/sparen.html"),
        ["nav.url.home"] = new("https://www.kbcbrussels.be/retail/en/themes/myhome.html",
            "https://www.kbcbrussels.be/particuliers/fr/themes/myhome.html",
            "https://www.kbcbrussels.be/particulieren/nl/thema/myhome.html"),
        ["nav.url.family"] = new("https://www.kbcbrussels.be/retail/en/insurance/family.html",
            "https://www.kbcbrussels.be/particuliers/fr/assurer/famille.html",
            "https://www.kbcbrussels.be/particulieren/nl/verzekeren/gezin.html"),
        ["nav.url.mobility"] = new("https://www.kbcbrussels.be/retail/en/themes/mymobility.html",
            "https://www.kbcbrussels.be/particuliers/fr/themes/mymobility.html",
            "https://www.kbcbrussels.be/particulieren/nl/thema/mymobility.html"),

        // Page names (top bar, headings, user menu)
        ["page.settings"] = new("Settings", "Paramètres", "Instellingen"),
        ["page.rules"] = new("Rules", "Règles", "Regels"),
        ["page.appearance"] = new("Appearance", "Apparence", "Weergave"),
        ["page.transactions"] = new("Transactions", "Transactions", "Transacties"),
        ["page.recurring"] = new("Recurring expenses", "Dépenses récurrentes", "Terugkerende uitgaven"),
        ["page.notifications"] = new("Notifications", "Notifications", "Meldingen"),
        ["page.email"] = new("Email reminders", "Rappels par e-mail", "E-mailherinneringen"),
        ["menu.darkMode"] = new("Dark mode", "Mode sombre", "Donkere modus"),
        ["menu.lightMode"] = new("Light mode", "Mode clair", "Lichte modus"),

        // Settings
        ["settings.weEmailYouWhen"] = new("We email you when:", "Nous vous envoyons un e-mail en cas de :", "We mailen je bij:"),
        ["settings.emailNotifications"] = new("Email notifications", "Notifications par e-mail", "E-mailmeldingen"),
        ["settings.channels"] = new("Notification channels", "Canaux de notification", "Meldingskanalen"),
        ["settings.email"] = new("Email", "E-mail", "E-mail"),
        ["settings.delivery"] = new("Delivery", "Envoi", "Verzending"),
        ["settings.deliveryInstant"] = new("Every alert instantly", "Chaque alerte immédiatement", "Elke melding meteen"),
        ["settings.deliveryDigest"] = new("Critical instantly, others in a daily summary at 18:00",
            "Critiques immédiatement, les autres dans un résumé quotidien à 18h00",
            "Kritieke meteen, de rest in een dagelijkse samenvatting om 18.00 uur"),
        ["settings.digestSwitch"] = new("Daily summary for regular alerts", "Résumé quotidien pour les alertes ordinaires", "Dagelijkse samenvatting voor gewone meldingen"),
        ["settings.channelsSaved"] = new("Notification channels saved", "Canaux de notification enregistrés", "Meldingskanalen opgeslagen"),
        ["settings.theme"] = new("Theme", "Thème", "Thema"),
        ["settings.light"] = new("Light", "Clair", "Licht"),
        ["settings.dark"] = new("Dark", "Sombre", "Donker"),
        ["settings.language"] = new("Language", "Langue", "Taal"),
        ["settings.languageHint"] = new("Used on every page and in your emails.", "Utilisée sur toutes les pages et dans vos e-mails.", "Gebruikt op elke pagina en in je e-mails."),

        // Rules page
        ["rules.critical.title"] = new("Critical payments only", "Paiements critiques uniquement", "Alleen kritieke betalingen"),
        ["rules.critical.desc"] = new(
            "Only email me about expenses I marked as critical, like rent, energy or insurance. Skip regular ones like groceries or subscriptions.",
            "Ne m'envoyer des e-mails que pour les dépenses marquées comme critiques, comme le loyer, l'énergie ou les assurances. Ignorer les dépenses ordinaires comme les courses ou les abonnements.",
            "Mail me alleen over uitgaven die ik als kritiek heb gemarkeerd, zoals huur, energie of verzekeringen. Sla gewone uitgaven zoals boodschappen of abonnementen over."),
        ["rules.critical.link"] = new("Choose critical expenses", "Choisir les dépenses critiques", "Kritieke uitgaven kiezen"),
        ["rules.critical.on"] = new("You'll only get emails about critical expenses", "Vous ne recevrez des e-mails que pour les dépenses critiques", "Je krijgt alleen nog e-mails over kritieke uitgaven"),
        ["rules.critical.off"] = new("You'll get emails about all expenses", "Vous recevrez des e-mails pour toutes les dépenses", "Je krijgt e-mails over alle uitgaven"),

        // Rules (names and descriptions, keyed by RuleInfo.Id)
        ["rule.price-increase"] = new("Price increase", "Hausse de prix", "Prijsstijging"),
        ["rule.price-increase.desc"] = new(
            "We email you when an expense costs more than your previous payment to the same company.",
            "Nous vous prévenons quand une dépense coûte plus cher que votre paiement précédent à la même entreprise.",
            "We mailen je wanneer een uitgave meer kost dan je vorige betaling aan hetzelfde bedrijf."),
        ["rule.recurring-added"] = new("New recurring expense", "Nouvelle dépense récurrente", "Nieuwe terugkerende uitgave"),
        ["rule.recurring-added.desc"] = new(
            "We email you when the same expense is paid on the same day two months in a row.",
            "Nous vous prévenons quand la même dépense est payée le même jour deux mois de suite.",
            "We mailen je wanneer dezelfde uitgave twee maanden na elkaar op dezelfde dag wordt betaald."),
        ["rule.recurring-removed"] = new("Recurring expense stopped", "Dépense récurrente arrêtée", "Terugkerende uitgave gestopt"),
        ["rule.recurring-removed.desc"] = new(
            "We email you when a recurring expense isn't paid the following month.",
            "Nous vous prévenons quand une dépense récurrente n'est pas payée le mois suivant.",
            "We mailen je wanneer een terugkerende uitgave de volgende maand niet wordt betaald."),
        ["rule.price-decrease"] = new("Price decrease", "Baisse de prix", "Prijsdaling"),
        ["rule.price-decrease.desc"] = new(
            "We email you when a subscription or recurring expense gets cheaper.",
            "Nous vous prévenons quand un abonnement ou une dépense récurrente devient moins cher.",
            "We mailen je wanneer een abonnement of terugkerende uitgave goedkoper wordt."),
        ["rule.payment-incomplete"] = new("Incomplete payment reminder", "Rappel de paiement incomplet", "Herinnering onvolledige betaling"),
        ["rule.payment-incomplete.desc"] = new(
            "Paid a bill in parts? We remind you about the part that's still open, based on how you split it.",
            "Vous avez payé une facture en plusieurs fois ? Nous vous rappelons la partie encore due, selon la répartition choisie.",
            "Een factuur in delen betaald? We herinneren je aan het deel dat nog openstaat, volgens hoe je het hebt opgesplitst."),
        ["rule.duplicate-charge"] = new("Possible double charge", "Double prélèvement possible", "Mogelijk dubbel aangerekend"),
        ["rule.duplicate-charge.desc"] = new(
            "We email you right away when the same company charges you the same amount twice within 2 days.",
            "Nous vous prévenons immédiatement quand une même entreprise vous débite deux fois le même montant en 2 jours.",
            "We mailen je meteen wanneer hetzelfde bedrijf je binnen 2 dagen twee keer hetzelfde bedrag aanrekent."),
        ["rule.upcoming-payment"] = new("Upcoming critical payment", "Paiement critique à venir", "Kritieke betaling op komst"),
        ["rule.upcoming-payment.desc"] = new(
            "We remind you 3 days before a critical payment, like rent or energy, is due.",
            "Nous vous rappelons 3 jours avant l'échéance d'un paiement critique, comme le loyer ou l'énergie.",
            "We herinneren je 3 dagen voor een kritieke betaling, zoals huur of energie, vervalt."),

        // Appearance
        ["appearance.title"] = new("Appearance & Personalization", "Apparence & personnalisation", "Weergave & personalisatie"),
        ["appearance.personalize"] = new("Personalize", "Personnaliser", "Personaliseren"),
        ["appearance.personalize.desc"] = new("Use your own theme and tone of speech in the app.", "Utilisez votre propre thème et ton dans l'application.", "Gebruik je eigen thema en aanspreektoon in de app."),
        ["appearance.personalization"] = new("Personalization", "Personnalisation", "Personalisatie"),
        ["appearance.dark.desc"] = new("Dark backgrounds with light text. Works with every theme.", "Fonds sombres et texte clair. Compatible avec tous les thèmes.", "Donkere achtergronden met lichte tekst. Werkt met elk thema."),
        ["appearance.email.desc"] = new(
            "Choose the tone, design and text style of the emails we send you. Now: {0}, {1}.",
            "Choisissez le ton, le design et le style de texte de nos e-mails. Actuellement : {0}, {1}.",
            "Kies de toon, het ontwerp en de tekststijl van onze e-mails. Nu: {0}, {1}."),
        ["appearance.themes"] = new("Themes", "Thèmes", "Thema's"),
        ["appearance.tone"] = new("Tone of speech", "Ton", "Aanspreektoon"),
        ["tone.Young"] = new("Young", "Jeune", "Jong"),
        ["tone.Professional"] = new("Professional", "Professionnel", "Professioneel"),
        ["tone.Joyful"] = new("Joyful", "Joyeux", "Vrolijk"),
        ["tone.Factual"] = new("Factual", "Factuel", "Zakelijk"),

        // Email reminders
        ["email.tone"] = new("Tone", "Ton", "Toon"),
        ["etone.Playful"] = new("Playful", "Ludique", "Speels"),
        ["etone.Informal"] = new("Informal", "Décontracté", "Informeel"),
        ["etone.Formal"] = new("Formal", "Formel", "Formeel"),
        ["etone.Serious"] = new("Serious", "Sérieux", "Ernstig"),
        ["etone.Panicky"] = new("Panicky", "Alarmiste", "Paniekerig"),
        ["etone.Playful.desc"] = new("Light and fun, with a wink.", "Léger et amusant, avec un clin d'œil.", "Luchtig en grappig, met een knipoog."),
        ["etone.Informal.desc"] = new("Friendly and short, like a message from a friend.", "Amical et bref, comme le message d'un ami.", "Vriendelijk en kort, zoals een berichtje van een vriend."),
        ["etone.Formal.desc"] = new("Polite and neutral, like a letter from your bank.", "Poli et neutre, comme un courrier de votre banque.", "Beleefd en neutraal, zoals een brief van je bank."),
        ["etone.Serious.desc"] = new("Straight to the facts and numbers.", "Droit aux faits et aux chiffres.", "Recht naar de feiten en cijfers."),
        ["etone.Panicky.desc"] = new("Loud and urgent, so you can't miss it.", "Fort et urgent, impossible à manquer.", "Luid en dringend, zodat je het niet mist."),
        ["email.design"] = new("Design", "Design", "Ontwerp"),
        ["design.Classic"] = new("Classic", "Classique", "Klassiek"),
        ["design.Minimal"] = new("Minimal", "Minimaliste", "Minimaal"),
        ["design.Theme"] = new("Theme", "Thème", "Thema"),
        ["design.Classic.desc"] = new("KBC header with a blue line and a solid button.", "En-tête KBC avec une ligne bleue et un bouton plein.", "KBC-koptekst met een blauwe lijn en een volle knop."),
        ["design.Minimal.desc"] = new("Plain and light: small logo, link instead of a button.", "Simple et léger : petit logo, lien au lieu d'un bouton.", "Sober en licht: klein logo, link in plaats van een knop."),
        ["design.Theme.desc"] = new("Header in your {0} theme.", "En-tête aux couleurs de votre thème {0}.", "Koptekst in je thema {0}."),
        ["design.Theme.none"] = new(
            "Header in KBC navy and blue. Pick a theme under Appearance to use it here.",
            "En-tête en bleu marine et bleu KBC. Choisissez un thème dans Apparence pour l'utiliser ici.",
            "Koptekst in KBC-marineblauw en -blauw. Kies een thema onder Weergave om het hier te gebruiken."),
        ["email.textStyle"] = new("Text style", "Style du texte", "Tekststijl"),
        ["email.textSize"] = new("Text size", "Taille du texte", "Tekstgrootte"),
        ["size.Small"] = new("Small", "Petite", "Klein"),
        ["size.Regular"] = new("Regular", "Normale", "Normaal"),
        ["size.Large"] = new("Large", "Grande", "Groot"),
        ["email.emoji"] = new("Use emoji", "Utiliser des emoji", "Emoji gebruiken"),
        ["email.preview"] = new("Preview", "Aperçu", "Voorbeeld"),
        ["email.from"] = new("From", "De", "Van"),
        ["email.to"] = new("To", "À", "Aan"),
        ["email.footer"] = new(
            "You receive this email because “{0}” is on. Manage your rules in the KBC Brussels app.",
            "Vous recevez cet e-mail car « {0} » est activé. Gérez vos règles dans l'app KBC Brussels.",
            "Je ontvangt deze e-mail omdat ‘{0}’ aanstaat. Beheer je regels in de KBC Brussels-app."),
        ["sample.PriceIncrease"] = new("Price increase", "Hausse de prix", "Prijsstijging"),
        ["sample.PriceDecrease"] = new("Price decrease", "Baisse de prix", "Prijsdaling"),
        ["sample.PaymentIncomplete"] = new("Incomplete payment", "Paiement incomplet", "Onvolledige betaling"),

        // Transactions
        ["tx.addTitle"] = new("Add mock transaction", "Ajouter une transaction fictive", "Testtransactie toevoegen"),
        ["tx.type"] = new("Type", "Type", "Type"),
        ["tx.Expense"] = new("Expense", "Dépense", "Uitgave"),
        ["tx.Income"] = new("Income", "Revenu", "Inkomen"),
        ["tx.company"] = new("Company", "Entreprise", "Bedrijf"),
        ["tx.amountField"] = new("Amount (€)", "Montant (€)", "Bedrag (€)"),
        ["tx.amount"] = new("Amount", "Montant", "Bedrag"),
        ["tx.date"] = new("Date", "Date", "Datum"),
        ["tx.add"] = new("Add", "Ajouter", "Toevoegen"),
        ["tx.added"] = new("Added: {0} at {1}", "Ajouté : {0} chez {1}", "Toegevoegd: {0} bij {1}"),
        ["tx.emailSent"] = new("Email sent: {0}", "E-mail envoyé : {0}", "E-mail verstuurd: {0}"),
        ["tx.digest"] = new("In today's summary (18:00): {0}", "Dans le résumé du jour (18h00) : {0}", "In de samenvatting van vandaag (18.00 uur): {0}"),
        ["tx.loadError"] = new("Could not load transactions: {0}", "Impossible de charger les transactions : {0}", "Kan transacties niet laden: {0}"),
        ["tx.addError"] = new("Could not add transaction: {0}", "Impossible d'ajouter la transaction : {0}", "Kan transactie niet toevoegen: {0}"),

        // Recurring expenses
        ["rec.intro"] = new("Mark what can't be missed as <strong>Critical</strong>.",
            "Marquez ce qui ne peut pas être oublié comme <strong>Critique</strong>.",
            "Markeer wat niet mag worden vergeten als <strong>Kritiek</strong>."),
        ["rec.intro.on"] = new("Critical payments only is on: you only get emails about critical expenses.",
            "« Paiements critiques uniquement » est activé : vous ne recevez des e-mails que pour les dépenses critiques.",
            "‘Alleen kritieke betalingen’ staat aan: je krijgt alleen e-mails over kritieke uitgaven."),
        ["rec.intro.off"] = new("Turn on Critical payments only in Rules to skip emails about regular expenses.",
            "Activez « Paiements critiques uniquement » dans Règles pour ne plus recevoir d'e-mails sur les dépenses ordinaires.",
            "Zet ‘Alleen kritieke betalingen’ aan in Regels om geen e-mails meer te krijgen over gewone uitgaven."),
        ["rec.critical"] = new("Critical", "Critique", "Kritiek"),
        ["rec.regular"] = new("Regular", "Ordinaire", "Gewoon"),
        ["rec.day"] = new("Day", "Jour", "Dag"),
        ["rec.since"] = new("Since", "Depuis", "Sinds"),
        ["rec.priority"] = new("Priority", "Priorité", "Prioriteit"),
        ["rec.priorityOf"] = new("Priority of {0}", "Priorité de {0}", "Prioriteit van {0}"),
        ["rec.empty"] = new("No recurring expenses yet. Add the same expense on the same day two months in a row.",
            "Pas encore de dépenses récurrentes. Ajoutez la même dépense le même jour deux mois de suite.",
            "Nog geen terugkerende uitgaven. Voeg twee maanden na elkaar dezelfde uitgave op dezelfde dag toe."),
        ["rec.marked"] = new("{0} marked as {1}", "{0} marqué comme {1}", "{0} gemarkeerd als {1}"),
        ["rec.loadError"] = new("Could not load recurring expenses: {0}", "Impossible de charger les dépenses récurrentes : {0}", "Kan terugkerende uitgaven niet laden: {0}"),
        ["rec.updateError"] = new("Could not update {0}: {1}", "Impossible de modifier {0} : {1}", "Kan {0} niet bijwerken: {1}"),

        // Notifications
        ["notif.subtitle"] = new("Every email the rules have sent.", "Tous les e-mails envoyés par les règles.", "Alle e-mails die de regels hebben verstuurd."),
        ["notif.empty"] = new("No notifications sent yet.", "Aucune notification envoyée pour l'instant.", "Nog geen meldingen verstuurd."),
        ["notif.digest"] = new("Daily summary", "Résumé quotidien", "Dagelijkse samenvatting"),
        ["notif.loadError"] = new("Could not load notifications: {0}", "Impossible de charger les notifications : {0}", "Kan meldingen niet laden: {0}"),

        // Login / sign-up
        ["auth.login"] = new("Log in", "Se connecter", "Aanmelden"),
        ["auth.signup"] = new("Sign up", "S'inscrire", "Registreren"),
        ["auth.name"] = new("Full name", "Nom complet", "Volledige naam"),
        ["auth.email"] = new("Email", "E-mail", "E-mail"),
        ["auth.password"] = new("Password", "Mot de passe", "Wachtwoord"),
        ["auth.confirm"] = new("Confirm password", "Confirmer le mot de passe", "Bevestig wachtwoord"),
        ["auth.submitSignup"] = new("Create account", "Créer un compte", "Account aanmaken"),
        ["auth.noAccount"] = new("No account yet?", "Pas encore de compte ?", "Nog geen account?"),
        ["auth.haveAccount"] = new("Already have an account?", "Vous avez déjà un compte ?", "Heb je al een account?"),
        ["auth.demo"] = new("Use demo account", "Utiliser le compte de démo", "Demo-account gebruiken"),
        ["auth.note"] = new("Demo environment with sample accounts. Never enter your real KBC credentials here.",
            "Environnement de démo avec des comptes fictifs. N'entrez jamais vos vrais identifiants KBC ici.",
            "Demo-omgeving met testaccounts. Vul hier nooit je echte KBC-gegevens in."),
        ["auth.err.required"] = new("Please fill in all fields.", "Veuillez remplir tous les champs.", "Vul alle velden in."),
        ["auth.err.email"] = new("Enter a valid email address.", "Saisissez une adresse e-mail valide.", "Vul een geldig e-mailadres in."),
        ["auth.err.invalid"] = new("Email or password is incorrect.", "E-mail ou mot de passe incorrect.", "E-mail of wachtwoord is onjuist."),
        ["auth.err.short"] = new("Password must be at least 8 characters.", "Le mot de passe doit contenir au moins 8 caractères.", "Het wachtwoord moet minstens 8 tekens bevatten."),
        ["auth.err.match"] = new("Passwords don't match.", "Les mots de passe ne correspondent pas.", "De wachtwoorden komen niet overeen."),
        ["auth.err.exists"] = new("An account with this email already exists.", "Un compte existe déjà avec cette adresse e-mail.", "Er bestaat al een account met dit e-mailadres."),
        ["menu.logout"] = new("Log out", "Se déconnecter", "Afmelden"),

        // Not found page
        ["notfound.title"] = new("Page not found", "Page introuvable", "Pagina niet gevonden"),
        ["notfound.text"] = new("Sorry, this page doesn't exist or has moved.", "Désolé, cette page n'existe pas ou a été déplacée.", "Sorry, deze pagina bestaat niet of is verplaatst."),
        ["notfound.back"] = new("Back to Settings", "Retour aux paramètres", "Terug naar Instellingen"),

        // Notification messages (written by the API in real life; the mock uses these)
        ["msg.priceUp"] = new("Your expense at {0} went up from {1} to {2}.", "Votre dépense chez {0} a augmenté de {1} à {2}.", "Je uitgave bij {0} is gestegen van {1} naar {2}."),
        ["msg.priceDown"] = new("Your expense at {0} went down from {1} to {2}.", "Votre dépense chez {0} a baissé de {1} à {2}.", "Je uitgave bij {0} is gedaald van {1} naar {2}."),
        ["msg.recAdded"] = new("{0} was added to your recurring expenses.", "{0} a été ajouté à vos dépenses récurrentes.", "{0} is toegevoegd aan je terugkerende uitgaven."),
        ["msg.recRemoved"] = new("{0} was removed from your recurring expenses.", "{0} a été retiré de vos dépenses récurrentes.", "{0} is verwijderd uit je terugkerende uitgaven."),
        ["msg.incomplete"] = new("You paid {1} of your {2} {0} bill. {3} is still open.",
            "Vous avez payé {1} sur votre facture {0} de {2}. Il reste {3} à payer.",
            "Je hebt {1} van je {0}-factuur van {2} betaald. Er staat nog {3} open."),
        ["msg.duplicate"] = new("{0} charged you {1} twice within 2 days. Was that intended?",
            "{0} vous a débité deux fois {1} en 2 jours. Est-ce normal ?",
            "{0} heeft je binnen 2 dagen twee keer {1} aangerekend. Klopt dat?"),
        ["msg.upcoming"] = new("Your {0} payment of {1} is due in 3 days.",
            "Votre paiement {0} de {1} arrive à échéance dans 3 jours.",
            "Je betaling aan {0} van {1} vervalt over 3 dagen."),
    };

    public static string Get(string key, Lang lang) =>
        All.TryGetValue(key, out var t) ? lang switch { Lang.Fr => t.Fr, Lang.Nl => t.Nl, _ => t.En } : key;
}

// Scoped per circuit: reads the language chosen in Settings.
public class Loc(UiState ui)
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-BE");
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-BE");
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-GB");

    public Lang Lang => ui.Language;

    // For MudBlazor inputs: French/Dutch users type "15,99", English users "15.99". Also localizes the date picker.
    public CultureInfo Culture => ui.Language switch { Lang.Fr => Fr, Lang.Nl => Nl, _ => En };

    public string this[string key] => Texts.Get(key, ui.Language);

    public string F(string key, params object[] args) => string.Format(this[key], args);

    public string Rule(RuleInfo rule) => this[$"rule.{rule.Id}"];

    public string RuleDesc(RuleInfo rule) => this[$"rule.{rule.Id}.desc"];

    public string PageTitle(string pageKey) => $"{this[pageKey]} – KBC Brussels";

    // Belgian conventions: "13,99 €" in French, "€ 13,99" in Dutch.
    public string Money(decimal value) => ui.Language switch
    {
        Lang.Fr => $"{value.ToString("#,##0.00", Fr)} €",
        Lang.Nl => $"€ {value.ToString("#,##0.00", Nl)}",
        _ => $"€{value.ToString("#,##0.00", En)}",
    };

    public static string Code(Lang lang) => lang switch { Lang.Fr => "fr", Lang.Nl => "nl", _ => "en" };
}
