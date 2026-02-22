using GESTION_PANIER.Data;
using GESTION_PANIER.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using projetwebtestmigration.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GESTION_PANIER.Pages
{
    public class ChatbotModel : PageModel
    {
        private readonly GESTION_PANIERContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        // Nouveau: Stockage des embeddings en mémoire
        private static List<ProductEmbedding> _productEmbeddings = new List<ProductEmbedding>();
        private static DateTime _embeddingsLastUpdated = DateTime.MinValue;

        public ChatbotModel(
            GESTION_PANIERContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _cache = cache;
        }

        [BindProperty]
        public string? UserMessage { get; set; }

        public string BotResponse { get; set; } = "";
        public List<Product> RelevantProducts { get; set; } = new List<Product>();
        public List<string> SearchKeywords { get; set; } = new List<string>();
        public string SearchMethod { get; set; } = "Enhanced Keyword";
        public float AverageSimilarity { get; set; } = 0;
        public string SystemStatus { get; set; } = "Optimisé";
        public long RagTimeMs { get; set; } = 0;
        public List<SimilarityResult> SimilarityResults { get; set; } = new List<SimilarityResult>();

        // Nouvelle classe pour stocker les résultats de similarité
        public class SimilarityResult
        {
            public string ProductName { get; set; } = "";
            public double SimilarityScore { get; set; }
            public string Category { get; set; } = "";
            public List<string> MatchedKeywords { get; set; } = new List<string>();
        }

        // Nouvelle classe pour les embeddings
        public class ProductEmbedding
        {
            public Product Product { get; set; } = new Product();
            public float[] Embedding { get; set; } = Array.Empty<float>();
            public string TextForEmbedding { get; set; } = "";
            public DateTime LastUpdated { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(UserMessage))
            {
                BotResponse = "Veuillez entrer un message.";
                return Page();
            }

            Console.WriteLine($"\n?? REQUÊTE : '{UserMessage}'");
            var metrics = new Dictionary<string, object>();
            var swTotal = Stopwatch.StartNew();

            // 1. Charger produits depuis cache (OPTIMISÉ)
            const string cacheKey = "products_all";
            List<Product> allProducts;

            var swCache = Stopwatch.StartNew();
            if (!_cache.TryGetValue(cacheKey, out allProducts))
            {
                Console.WriteLine("?? Chargement produits depuis la base de données...");
                allProducts = await _context.Product
                    .Include(p => p.Category)
                    .Where(p => !string.IsNullOrEmpty(p.Name))
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, allProducts, TimeSpan.FromHours(2));
                metrics["CacheHit"] = false;
                Console.WriteLine($"? {allProducts.Count} produits chargés");
            }
            else
            {
                metrics["CacheHit"] = true;
                Console.WriteLine($"? Produits depuis cache: {allProducts.Count}");
            }
            swCache.Stop();
            metrics["LoadProductsCacheTime_ms"] = swCache.ElapsedMilliseconds;
            metrics["TotalProducts"] = allProducts.Count;

            // NOUVEAU: Générer/charger les embeddings
            var swEmbeddings = Stopwatch.StartNew();
            await GenerateOrLoadEmbeddingsAsync(allProducts);
            swEmbeddings.Stop();
            metrics["EmbeddingsTime_ms"] = swEmbeddings.ElapsedMilliseconds;

            // 2. VÉRIFICATION RAPIDE DE L'API (avec timeout court)
            bool apiAvailable = await QuickApiCheck();
            metrics["ApiAvailable"] = apiAvailable;

            // Démarrer le chronomètre RAG
            var ragTimer = Stopwatch.StartNew();

            if (!apiAvailable)
            {
                Console.WriteLine("?? API non disponible - Mode RAG Local activé");
                SystemStatus = "Mode RAG Local (API indisponible)";

                // Utiliser notre moteur RAG local avec embeddings
                SearchMethod = "Recherche Sémantique Locale (Embeddings)";
                await SemanticLocalSearchAsync(UserMessage, allProducts, metrics);
            }
            else
            {
                Console.WriteLine("? API disponible");
                SystemStatus = "Mode Complet (API disponible)";

                // Détecter le type de requête
                var queryType = AnalyzeQueryType(UserMessage);
                metrics["QueryType"] = queryType;

                if (queryType == "semantic" && _productEmbeddings.Count > 3)
                {
                    SearchMethod = "Recherche Sémantique (Embeddings + Cosinus)";
                    await SemanticSearchWithEmbeddingsAsync(UserMessage, allProducts, metrics);
                }
                else
                {
                    SearchMethod = "Recherche Hybride (Embeddings + Keywords)";
                    await HybridSearchAsync(UserMessage, allProducts, metrics);
                }
            }

            metrics["SearchMethod"] = SearchMethod;
            metrics["ProductsFound"] = RelevantProducts.Count;

            // 3. Construire le contexte RAG enrichi
            var swContext = Stopwatch.StartNew();
            string ragContext = BuildEnhancedRagContext(RelevantProducts, allProducts, UserMessage);
            swContext.Stop();
            metrics["ContextBuildTime_ms"] = swContext.ElapsedMilliseconds;
            metrics["ContextLength"] = ragContext.Length;

            // Arrêter le chronomètre RAG
            ragTimer.Stop();
            RagTimeMs = ragTimer.ElapsedMilliseconds;
            metrics["RAG_TIME_ms"] = RagTimeMs;

            // 4. GÉNÉRER LA RÉPONSE
            var swResponse = Stopwatch.StartNew();

            if (apiAvailable)
            {
                // Utiliser l'API LLM si disponible
                try
                {
                    string promptEnrichi = BuildOptimizedPrompt(ragContext, UserMessage, SearchMethod, SimilarityResults);
                    metrics["PromptLength"] = promptEnrichi.Length;

                    BotResponse = await CallLlmApiAsync(promptEnrichi);
                    Console.WriteLine($"? Réponse LLM générée");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"?? Erreur LLM: {ex.Message}");
                    BotResponse = GenerateLocalResponse(RelevantProducts, UserMessage, allProducts, SearchMethod);
                }
            }
            else
            {
                // Générer une réponse locale intelligente
                BotResponse = GenerateLocalResponse(RelevantProducts, UserMessage, allProducts, SearchMethod);
                Console.WriteLine($"? Réponse locale générée");
            }

            swResponse.Stop();
            metrics["ResponseTime_ms"] = swResponse.ElapsedMilliseconds;

            // 5. Final
            swTotal.Stop();
            metrics["TotalTime_ms"] = swTotal.ElapsedMilliseconds;

            // Log optimisé
            Console.WriteLine("\n?? === MÉTRIQUES PERFORMANCE ===");
            Console.WriteLine($"? Temps total: {metrics["TotalTime_ms"]}ms");
            Console.WriteLine($"?? RAG_TIME: {RagTimeMs}ms");
            Console.WriteLine($"?? Méthode: {SearchMethod}");
            Console.WriteLine($"?? Produits trouvés: {RelevantProducts.Count}");
            Console.WriteLine($"?? Similarité moyenne: {AverageSimilarity:F2}");
            Console.WriteLine($"?? Statut: {SystemStatus}");
            Console.WriteLine("===============================\n");

            return Page();
        }

        // ============================
        // NOUVEAU: GÉNÉRATION D'EMBEDDINGS LOCALE
        // ============================

        private async Task GenerateOrLoadEmbeddingsAsync(List<Product> products)
        {
            // Vérifier si les embeddings sont à jour
            if (_productEmbeddings.Count == products.Count &&
                (DateTime.Now - _embeddingsLastUpdated).TotalHours < 2)
            {
                Console.WriteLine($"? Embeddings déjà chargés: {_productEmbeddings.Count}");
                return;
            }

            Console.WriteLine("?? Génération des embeddings locaux...");

            _productEmbeddings.Clear();
            var sw = Stopwatch.StartNew();

            // Simuler la génération d'embeddings pour chaque produit
            foreach (var product in products)
            {
                var textForEmbedding = GenerateEmbeddingText(product);
                var embedding = GenerateSimpleEmbedding(textForEmbedding, product.Id);

                _productEmbeddings.Add(new ProductEmbedding
                {
                    Product = product,
                    Embedding = embedding,
                    TextForEmbedding = textForEmbedding,
                    LastUpdated = DateTime.Now
                });
            }

            _embeddingsLastUpdated = DateTime.Now;
            sw.Stop();
            Console.WriteLine($"? Embeddings générés: {_productEmbeddings.Count} produits ({sw.ElapsedMilliseconds}ms)");
        }

        private string GenerateEmbeddingText(Product product)
        {
            // Créer un texte riche pour générer des embeddings
            var sb = new StringBuilder();

            sb.Append(product.Name?.ToLower() ?? "");
            sb.Append(" ");
            sb.Append(product.Description?.ToLower() ?? "");
            sb.Append(" ");
            sb.Append(product.Category?.Name?.ToLower() ?? "");
            sb.Append(" ");

            // Ajouter des caractéristiques basées sur le nom
            var name = product.Name?.ToLower() ?? "";
            if (name.Contains("hydrat")) sb.Append("hydratant nourrissant peau sèche ");
            if (name.Contains("nettoy")) sb.Append("nettoyant purifiant propre ");
            if (name.Contains("crème")) sb.Append("crème émulsion riche ");
            if (name.Contains("gel")) sb.Append("gel léger frais ");
            if (name.Contains("tonique")) sb.Append("tonique rafraîchissant ");
            if (name.Contains("masque")) sb.Append("masque traitement intensif ");
            if (name.Contains("sérum")) sb.Append("sérum concentré actif ");

            return sb.ToString().Trim();
        }

        private float[] GenerateSimpleEmbedding(string text, int seed)
        {
            // Simuler un embedding simple basé sur le texte
            // Dans un vrai système, vous utiliseriez SentenceTransformers ou un service d'embedding
            var rnd = new Random(seed);
            var embedding = new float[128]; // Embedding de taille fixe

            // Générer un embedding "sémantique" simple basé sur les mots
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < embedding.Length; i++)
            {
                float value = 0;

                // Baser la valeur sur la présence de certains mots-clés
                foreach (var word in words)
                {
                    // Hash simple du mot pour affecter différentes dimensions
                    var wordHash = Math.Abs(word.GetHashCode());
                    var dimension = wordHash % embedding.Length;

                    if (dimension == i)
                    {
                        value += 0.1f;
                    }
                }

                // Ajouter un peu de bruit
                value += (float)rnd.NextDouble() * 0.01f;
                embedding[i] = value;
            }

            // Normaliser l'embedding
            var norm = Math.Sqrt(embedding.Sum(x => x * x));
            if (norm > 0)
            {
                for (int i = 0; i < embedding.Length; i++)
                {
                    embedding[i] = (float)(embedding[i] / norm);
                }
            }

            return embedding;
        }

        // ============================
        // NOUVEAU: SIMILARITÉ COSINUS
        // ============================

        private float CalculateCosineSimilarity(float[] embedding1, float[] embedding2)
        {
            if (embedding1.Length != embedding2.Length)
                return 0;

            float dotProduct = 0;
            float norm1 = 0;
            float norm2 = 0;

            for (int i = 0; i < embedding1.Length; i++)
            {
                dotProduct += embedding1[i] * embedding2[i];
                norm1 += embedding1[i] * embedding1[i];
                norm2 += embedding2[i] * embedding2[i];
            }

            if (norm1 == 0 || norm2 == 0)
                return 0;

            return dotProduct / (float)(Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }

        private float[] GenerateQueryEmbedding(string query)
        {
            // Générer un embedding pour la requête
            var queryLower = query.ToLower();
            var embedding = new float[128];
            var rnd = new Random(query.GetHashCode());

            var words = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < embedding.Length; i++)
            {
                float value = 0;

                foreach (var word in words)
                {
                    var wordHash = Math.Abs(word.GetHashCode());
                    var dimension = wordHash % embedding.Length;

                    if (dimension == i)
                    {
                        // Plus de poids pour les mots significatifs
                        value += IsSignificantWord(word) ? 0.3f : 0.1f;
                    }
                }

                value += (float)rnd.NextDouble() * 0.01f;
                embedding[i] = value;
            }

            // Normaliser
            var norm = Math.Sqrt(embedding.Sum(x => x * x));
            if (norm > 0)
            {
                for (int i = 0; i < embedding.Length; i++)
                {
                    embedding[i] = (float)(embedding[i] / norm);
                }
            }

            return embedding;
        }

        private bool IsSignificantWord(string word)
        {
            // Liste des mots significatifs pour les cosmétiques
            var significantWords = new HashSet<string>
            {
                "hydrat", "nettoy", "crème", "gel", "tonique", "masque",
                "sérum", "visage", "corps", "cheveux", "peau", "soin",
                "beauté", "cosmétique", "maquillage", "protection", "anti",
                "sensible", "normale", "grasse", "sèche", "mixte"
            };

            return significantWords.Any(w => word.Contains(w));
        }

        // ============================
        // NOUVEAU: RECHERCHE SÉMANTIQUE AVEC EMBEDDINGS
        // ============================

        private async Task SemanticSearchWithEmbeddingsAsync(string query, List<Product> allProducts, Dictionary<string, object> metrics)
        {
            var swSearch = Stopwatch.StartNew();
            Console.WriteLine("?? Recherche sémantique avec embeddings et similarité cosinus...");

            // Générer l'embedding pour la requête
            var queryEmbedding = GenerateQueryEmbedding(query);

            // Calculer la similarité avec tous les produits
            var similarityResults = new List<(Product Product, float Similarity, ProductEmbedding Embedding)>();

            foreach (var productEmbedding in _productEmbeddings)
            {
                var similarity = CalculateCosineSimilarity(queryEmbedding, productEmbedding.Embedding);

                if (similarity > 0.3) // Seuil de similarité
                {
                    similarityResults.Add((productEmbedding.Product, similarity, productEmbedding));
                }
            }

            // Trier par similarité décroissante
            similarityResults = similarityResults
                .OrderByDescending(r => r.Similarity)
                .ToList();

            // Stocker les résultats de similarité pour l'affichage
            SimilarityResults = similarityResults
                .Select(r => new SimilarityResult
                {
                    ProductName = r.Product.Name ?? "",
                    SimilarityScore = Math.Round(r.Similarity, 4),
                    Category = r.Product.Category?.Name ?? "",
                    MatchedKeywords = ExtractMatchedKeywords(query, r.Embedding.TextForEmbedding)
                })
                .ToList();

            // Sélectionner les meilleurs produits
            RelevantProducts = similarityResults
                .Take(5)
                .Select(r => r.Product)
                .ToList();

            // Calculer la similarité moyenne
            AverageSimilarity = similarityResults.Any() ?
                similarityResults.Average(r => r.Similarity) : 0;

            // Extraire les mots-clés de la requête pour l'affichage
            SearchKeywords = ExtractEnhancedKeywords(query);

            swSearch.Stop();
            metrics["SemanticSearchTime_ms"] = swSearch.ElapsedMilliseconds;
            Console.WriteLine($"? {RelevantProducts.Count} produits trouvés (similarité cosinus moyenne: {AverageSimilarity:F2})");
        }

        private List<string> ExtractMatchedKeywords(string query, string productText)
        {
            var queryKeywords = ExtractEnhancedKeywords(query);
            var matched = new List<string>();

            foreach (var keyword in queryKeywords)
            {
                if (productText.Contains(keyword))
                {
                    matched.Add(keyword);
                }
            }

            return matched.Distinct().Take(3).ToList();
        }

        private async Task SemanticLocalSearchAsync(string query, List<Product> allProducts, Dictionary<string, object> metrics)
        {
            // Version locale simplifiée
            await SemanticSearchWithEmbeddingsAsync(query, allProducts, metrics);
        }

        private async Task HybridSearchAsync(string query, List<Product> allProducts, Dictionary<string, object> metrics)
        {
            var swSearch = Stopwatch.StartNew();
            Console.WriteLine("?? Recherche hybride (embeddings + mots-clés)...");

            var queryEmbedding = GenerateQueryEmbedding(query);
            var queryKeywords = ExtractEnhancedKeywords(query);
            SearchKeywords = queryKeywords;

            var combinedResults = new List<(Product Product, double Score)>();

            foreach (var productEmbedding in _productEmbeddings)
            {
                // Score sémantique (embeddings)
                var semanticScore = CalculateCosineSimilarity(queryEmbedding, productEmbedding.Embedding);

                // Score par mots-clés
                var keywordScore = CalculateEnhancedRelevance(productEmbedding.Product, queryKeywords, query).relevance;

                // Score combiné (pondération)
                var combinedScore = (semanticScore * 0.7) + (keywordScore * 0.3);

                if (combinedScore > 0.2)
                {
                    combinedResults.Add((productEmbedding.Product, combinedScore));
                }
            }

            RelevantProducts = combinedResults
                .OrderByDescending(r => r.Score)
                .Take(5)
                .Select(r => r.Product)
                .ToList();

            AverageSimilarity = combinedResults.Any() ?
                (float)combinedResults.Average(r => r.Score) : 0;

            swSearch.Stop();
            metrics["HybridSearchTime_ms"] = swSearch.ElapsedMilliseconds;
            Console.WriteLine($"? {RelevantProducts.Count} produits trouvés (score hybride moyen: {AverageSimilarity:F2})");
        }

        // ============================
        // PROMPT ENRICHI AVEC INFORMATIONS DE SIMILARITÉ
        // ============================

        private string BuildOptimizedPrompt(string ragContext, string userMessage, string searchMethod, List<SimilarityResult> similarityResults)
        {
            var similarityInfo = new StringBuilder();

            if (similarityResults.Any())
            {
                similarityInfo.AppendLine("\n**?? ANALYSE DE SIMILARITÉ COSINUS :**");
                foreach (var result in similarityResults.Take(3))
                {
                    similarityInfo.AppendLine($"- {result.ProductName}: {result.SimilarityScore:P1} de similarité");
                    if (result.MatchedKeywords.Any())
                    {
                        similarityInfo.AppendLine($"  Mots-clés correspondants: {string.Join(", ", result.MatchedKeywords)}");
                    }
                }
            }

            return $$"""
Tu es un assistant e-commerce expert en cosmétiques et produits de beauté.

{{ragContext}}

{{similarityInfo}}

**DEMANDE DE L'UTILISATEUR :**
"{{userMessage}}"

RÈGLES IMPORTANTES :
- Ne jamais répondre à des questions privées ou sensibles concernant les utilisateurs :
  ? Quantité d'articles dans le panier
  ? Prix ou total du panier
  ? Historique d'achats
- Ne réponds qu'à des questions générales sur les produits et leur disponibilité :
  ? Nom des produits
  ? Existence d'un produit
  ? Catégories de produits
  ? Produits populaires
- Si la question de l'utilisateur est hors contexte e-commerce :
  ? Réponds par UNE SEULE phrase simple.
  ? NE fais AUCUNE recommandation produit.
""";
        }

        // ============================
        // MÉTHODES EXISTANTES (conservées avec corrections mineures)
        // ============================

        private async Task<bool> QuickApiCheck()
        {
            var apiKey = _configuration["LLM:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("? Clé API non configurée");
                return false;
            }

            try
            {
                using var tcpClient = new TcpClient();
                var task = tcpClient.ConnectAsync("api.groq.com", 443);

                if (await Task.WhenAny(task, Task.Delay(2000)) == task)
                {
                    await task;
                    tcpClient.Close();
                    return true;
                }
                else
                {
                    Console.WriteLine("?? Timeout connexion TCP à l'API");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Connexion API échouée: {ex.Message}");
                return false;
            }
        }

        private string AnalyzeQueryType(string query)
        {
            query = query.ToLower();

            var semanticPatterns = new[]
            {
                "recommand", "suggér", "meilleur", "idéal", "pourquoi", "compar",
                "différence", "avantage", "inconvénient", "conseil", "aide",
                "que choisir", "quel est le", "quelle est la", "lequel", "laquelle",
                "similaire", "ressemble", "comme", "équivalent", "alternative"
            };

            if (semanticPatterns.Any(p => query.Contains(p)))
                return "semantic";

            var descriptivePatterns = new[]
            {
                "description", "décris", "caractéristique", "détail", "spécification",
                "qu'est-ce que", "c'est quoi", "présente", "montre", "explique"
            };

            if (descriptivePatterns.Any(p => query.Contains(p)))
                return "descriptive";

            return "general";
        }

        private (double relevance, int matchCount) CalculateEnhancedRelevance(Product product, List<string> keywords, string originalQuery)
        {
            if (keywords.Count == 0) return (0, 0);

            var productText = $"{product.Name} {product.Description} {product.Category?.Name}".ToLower();
            var queryLower = originalQuery.ToLower();

            double score = 0;
            int matchCount = 0;

            if (queryLower.Contains(product.Name.ToLower()))
            {
                score += 5.0;
                matchCount++;
            }

            foreach (var keyword in keywords)
            {
                if (productText.Contains(keyword))
                {
                    matchCount++;

                    if (product.Name.ToLower().Contains(keyword))
                        score += 3.0;
                    else if (product.Category?.Name?.ToLower().Contains(keyword) == true)
                        score += 2.0;
                    else if (product.Description?.ToLower().Contains(keyword) == true)
                        score += 1.5;
                    else
                        score += 1.0;
                }
            }

            if (!string.IsNullOrEmpty(product.Description) && product.Description.Length > 50)
            {
                score += 0.5;
            }

            double normalizedScore = score / (keywords.Count * 2);
            return (Math.Min(1.0, normalizedScore), matchCount);
        }

        private List<string> ExtractEnhancedKeywords(string query)
        {
            query = query.ToLower().Trim();

            var commonPhrases = new[]
            {
                "donner moi", "donne moi", "je veux", "j'aimerais", "peux-tu", "pouvez-vous",
                "s'il te plaît", "s'il vous plaît", "est-ce que", "avoir", "obtenir"
            };

            foreach (var phrase in commonPhrases)
            {
                query = query.Replace(phrase, "").Trim();
            }

            char[] separators = new char[] { ' ', ',', '.', ';', '!', '?', ':', '-', '(', ')', '[', ']' };

            var keywords = query
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 2)
                .Where(word => !IsCommonWord(word))
                .Distinct()
                .ToList();

            var expandedKeywords = new List<string>(keywords);
            foreach (var keyword in keywords)
            {
                if (EnhancedSynonymMap.TryGetValue(keyword, out var synonyms))
                {
                    expandedKeywords.AddRange(synonyms);
                }

                if (keyword.EndsWith("ant"))
                {
                    expandedKeywords.Add(keyword.Replace("ant", "ante"));
                }
                else if (keyword.EndsWith("eux"))
                {
                    expandedKeywords.Add(keyword.Replace("eux", "euse"));
                }
            }

            return expandedKeywords.Distinct().ToList();
        }

        private bool IsCommonWord(string word)
        {
            var commonWords = new HashSet<string>
            {
                "donner", "moi", "une", "des", "les", "du", "de", "la", "le", "et", "est",
                "que", "dans", "pour", "avec", "sur", "par", "au", "aux", "un", "une",
                "mon", "ton", "son", "notre", "votre", "leur", "ce", "cette", "ces", "cet",
                "je", "tu", "il", "elle", "nous", "vous", "ils", "elles", "qui", "quoi",
                "où", "quand", "comment", "pourquoi", "combien", "quel", "quelle", "quels",
                "quelles", "avoir", "être", "faire", "dire", "voir", "savoir", "pouvoir"
            };

            return commonWords.Contains(word);
        }

        private static readonly Dictionary<string, List<string>> EnhancedSynonymMap = new()
        {
            ["lotion"] = new List<string> { "lotion", "tonique", "solution", "fluide" },
            ["tonique"] = new List<string> { "rafraîchissant", "revitalisant", "stimulant" },
            ["hydratante"] = new List<string> { "hydratant", "nourrissant", "moisturizing", "humidifiant" },
            ["description"] = new List<string> { "détails", "caractéristiques", "spécifications", "informations", "présentation" },
            ["produit"] = new List<string> { "article", "item", "marchandise", "cosmétique", "soin" },
            ["visage"] = new List<string> { "face", "peau", "derme", "épiderme" },
            ["corps"] = new List<string> { "body", "peau", "épiderme", "tégument" },
            ["nettoyant"] = new List<string> { "cleaner", "purifiant", "dégraissant", "lavant", "démaquillant" },
            ["crème"] = new List<string> { "émulsion", "pommade", "baume", "onguent" },
            ["gel"] = new List<string> { "gelée", "substance", "préparation" }
        };

        private string BuildEnhancedRagContext(List<Product> relevantProducts, List<Product> allProducts, string query)
        {
            var context = new StringBuilder();

            context.AppendLine("## ?? INFORMATIONS PRODUITS (ANALYSE SÉMANTIQUE)\n");
            context.AppendLine($"**Recherche :** \"{query}\"\n");
            context.AppendLine($"**Méthode de recherche :** {SearchMethod}\n");
            context.AppendLine($"**Score de similarité moyen :** {AverageSimilarity:P1}\n");

            if (SimilarityResults.Any())
            {
                context.AppendLine("### ?? ANALYSE DE SIMILARITÉ DÉTAILLÉE\n");

                foreach (var result in SimilarityResults.Take(5))
                {
                    context.AppendLine($"#### {result.ProductName}");
                    context.AppendLine($"**Score de similarité :** {result.SimilarityScore:P1}");
                    context.AppendLine($"**Catégorie :** {result.Category}");
                    if (result.MatchedKeywords.Any())
                    {
                        context.AppendLine($"**Mots-clés correspondants :** {string.Join(", ", result.MatchedKeywords)}");
                    }
                    context.AppendLine();
                }
            }

            if (relevantProducts.Any())
            {
                context.AppendLine("### ?? PRODUITS PERTINENTS (TRIÉS PAR PERTINENCE)\n");

                foreach (var product in relevantProducts)
                {
                    var similarityResult = SimilarityResults.FirstOrDefault(s => s.ProductName == product.Name);
                    var similarityText = similarityResult != null ?
                        $"**[Similarité: {similarityResult.SimilarityScore:P1}]** " : "";

                    context.AppendLine($"#### {similarityText}{product.Name}\n");
                    context.AppendLine($"**?? Catégorie :** {product.Category?.Name ?? "Soins cosmétiques"}");
                    context.AppendLine($"**?? Prix :** {product.Price:C}");

                    if (!string.IsNullOrEmpty(product.Description))
                    {
                        context.AppendLine($"**?? Description sémantique :**");
                        context.AppendLine($"{FormatDescription(product.Description)}");
                    }

                    context.AppendLine();
                    context.AppendLine("---");
                    context.AppendLine();
                }
            }
            else
            {
                context.AppendLine("### ?? TOUS NOS PRODUITS\n");
                context.AppendLine($"*Aucune correspondance sémantique forte pour \"{query}\". Voici notre sélection :*\n");

                int count = 0;
                foreach (var product in allProducts.OrderBy(p => p.Name))
                {
                    if (count >= 8) break;

                    context.AppendLine($"• **{product.Name}**");
                    context.AppendLine($"  - Catégorie : {product.Category?.Name ?? "Général"}");
                    context.AppendLine($"  - Prix : {product.Price:C}");

                    if (!string.IsNullOrEmpty(product.Description))
                    {
                        var shortDesc = product.Description.Length > 80
                            ? product.Description.Substring(0, 80) + "..."
                            : product.Description;
                        context.AppendLine($"  - Description : {shortDesc}");
                    }

                    context.AppendLine();
                    count++;
                }
            }

            context.AppendLine($"\n---\n");
            context.AppendLine($"**?? Statistiques de recherche :**");
            context.AppendLine($"- Total produits analysés : {allProducts.Count}");
            context.AppendLine($"- Produits sémantiquement pertinents : {relevantProducts.Count}");
            context.AppendLine($"- Similarité moyenne détectée : {AverageSimilarity:P1}");
            context.AppendLine($"- Mots-clés extraits : {string.Join(", ", SearchKeywords.Take(5))}");

            return context.ToString();
        }

        private string FormatDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
                return "Aucune description disponible.";

            description = description.Trim();

            if (description.Length > 100 && description.Contains(","))
            {
                var parts = description.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 2)
                {
                    return string.Join("\n• ", parts.Select(p => p.Trim()));
                }
            }

            return description;
        }

        private string GenerateLocalResponse(List<Product> relevantProducts, string query, List<Product> allProducts, string searchMethod)
        {
            var response = new StringBuilder();

            response.AppendLine("## ?? Assistant Cosmétique - Mode RAG Local\n");
            response.AppendLine($"*Pour votre recherche :* \"{query}\"\n");
            response.AppendLine($"*Analyse sémantique utilisée :* {searchMethod}\n");

            if (relevantProducts.Any())
            {
                response.AppendLine("### ?? RÉSULTATS SÉMANTIQUES\n");

                foreach (var product in relevantProducts)
                {
                    var similarityResult = SimilarityResults.FirstOrDefault(s => s.ProductName == product.Name);

                    response.AppendLine($"#### {product.Name}");

                    if (similarityResult != null)
                    {
                        response.AppendLine($"**Pertinence sémantique :** {similarityResult.SimilarityScore:P1}");
                    }

                    response.AppendLine($"**?? Catégorie :** {product.Category?.Name ?? "Soins cosmétiques"}");
                    response.AppendLine($"**?? Prix :** {product.Price:C}");

                    if (!string.IsNullOrEmpty(product.Description))
                    {
                        response.AppendLine($"**?? Description :**");
                        response.AppendLine($"{product.Description}");
                    }

                    if (similarityResult?.MatchedKeywords.Any() == true)
                    {
                        response.AppendLine($"**?? Correspondances :** {string.Join(", ", similarityResult.MatchedKeywords)}");
                    }

                    response.AppendLine();
                }

                response.AppendLine("---\n");
                response.AppendLine("**?? Conseils basés sur l'analyse sémantique :**");

                if (AverageSimilarity > 0.7)
                {
                    response.AppendLine("- Excellente correspondance sémantique détectée");
                    response.AppendLine("- Les produits suggérés sont très pertinents pour votre recherche");
                }
                else if (AverageSimilarity > 0.4)
                {
                    response.AppendLine("- Bonne correspondance sémantique");
                    response.AppendLine("- Les produits partagent des caractéristiques avec votre recherche");
                }

                if (relevantProducts.Any(p => p.Name.ToLower().Contains("hydrat")))
                {
                    response.AppendLine("- Pour une hydratation optimale, appliquez sur peau propre matin et soir");
                }
            }
            else
            {
                response.AppendLine("### ?? NOTRE SÉLECTION DE PRODUITS\n");
                response.AppendLine("*Analyse sémantique en cours... Voici nos produits phares :*\n");

                foreach (var product in allProducts.Take(5))
                {
                    response.AppendLine($"• **{product.Name}**");
                    response.AppendLine($"  - {product.Category?.Name ?? "Produit cosmétique"}");
                    response.AppendLine($"  - {product.Price:C}");
                    response.AppendLine();
                }
            }

            response.AppendLine("---\n");
            response.AppendLine("**?? Système RAG Local :** Analyse sémantique activée avec embeddings");
            response.AppendLine($"**?? Score moyen :** {AverageSimilarity:P1}");
            response.AppendLine("**?? Astuce :** Plus votre recherche est descriptive, meilleure sera l'analyse sémantique.");

            return response.ToString();
        }

        private async Task<string> CallLlmApiAsync(string prompt)
        {
            var apiKey = _configuration["LLM:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("Clé API LLM manquante");

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.Timeout = TimeSpan.FromSeconds(25);

                var body = new
                {
                    model = "openai/gpt-oss-120b",
                    temperature = 0.4,
                    max_tokens = 800,
                    messages = new[]
                    {
                        new {
                            role = "system",
                            content = "Tu es un assistant e-commerce français spécialisé en cosmétiques. Tu es précis, utile et toujours courtois."
                        },
                        new {
                            role = "user",
                            content = prompt
                        }
                    }
                };

                var json = JsonSerializer.Serialize(body);
                var response = await client.PostAsync("chat/completions",
                    new StringContent(json, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Erreur API: {response.StatusCode}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);

                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Je n'ai pas pu générer de réponse détaillée.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Erreur API LLM: {ex.Message}");
                throw;
            }
        }

        public async Task<IActionResult> OnGetStatus()
        {
            var status = new StringBuilder();
            status.AppendLine("=== SYSTÈME RAG AVEC EMBEDDINGS - ÉTAT ===\n");

            status.AppendLine("?? Test connexion API:");
            var apiTest = await QuickApiCheck();
            status.AppendLine(apiTest ? "   ? API accessible" : "   ? API non accessible");

            status.AppendLine("\n?? Embeddings en mémoire:");
            status.AppendLine($"   Produits avec embeddings: {_productEmbeddings.Count}");
            status.AppendLine($"   Dernière mise à jour: {_embeddingsLastUpdated}");

            status.AppendLine("\n?? Base de données:");
            var products = await _context.Product.CountAsync();
            status.AppendLine($"   Produits enregistrés: {products}");

            status.AppendLine("\n?? Cache système:");
            if (_cache.TryGetValue("products_all", out List<Product> cachedProducts))
            {
                status.AppendLine($"   Produits en cache: {cachedProducts?.Count ?? 0}");
            }
            else
            {
                status.AppendLine("   Cache vide");
            }

            return Content(status.ToString(), "text/plain");
        }

        public string GetFormattedRagTime()
        {
            return $"{RagTimeMs} ms";
        }
    }
}