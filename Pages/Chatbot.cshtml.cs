using GESTION_PANIER.Data;
using GESTION_PANIER.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ChatbotModel : PageModel
{
    private readonly GESTION_PANIERContext _context;
    private readonly HuggingFaceLlmService _llm;
    private readonly CartSessionService _cartService;

    public ChatbotModel(GESTION_PANIERContext context, HuggingFaceLlmService llm, CartSessionService cartService)
    {
        _context = context;
        _llm = llm;
        _cartService = cartService;
    }

    [BindProperty]
    public string UserMessage { get; set; }

    public string BotResponse { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        // 1?? Récupérer le panier depuis ton service
        var cart = _cartService.GetCart();
        string cartSummary = string.Join(", ", cart.Items.Select(i =>
            $"ProduitID:{i.ProductId} Quantité:{i.Quantity}"));

        // 2?? Produits populaires
        var topProducts = _context.Product
            .OrderByDescending(p => p.SearchCount)
            .Take(3)
            .Select(p => p.Name)
            .ToList();

        // 3?? Construire le contexte pour le LLM
        string contexte = $@"
Message utilisateur : {UserMessage}

Panier (cookies) : {cartSummary}

Produits populaires : {string.Join(", ", topProducts)}

Fais des recommandations personnalisées.
";

        // 4?? Appel LLM
        BotResponse = await _llm.GetRecommendationAsync(contexte);

        return Page();
    }
}
