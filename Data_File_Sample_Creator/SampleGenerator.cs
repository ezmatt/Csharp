using System.Reflection;

public class SampleGenerator
{
    public List<Sample> Samples { get; private set; }
    public SampleGenerator()
    {
        Samples = new List<Sample>();
    }

    public void GenerateSamples(IEnumerable<FieldDefinition> fieldDefinitions)
    {
        // Iterate through the field definitions and populate the Samples dictionary
        foreach (FieldDefinition fd in fieldDefinitions)
        {
            Sample sample = new();
            
            // Default sample amount per field is 2, or get the value from the samples spreadsheet
            sample.Amount = 2;
            if (Int32.TryParse(fd.DataExample, out int parsedAmount))
            {
                sample.Amount = parsedAmount;
            }

            // Check if there are specific values required for each field
            if (fd.PossibleValues != null && fd.PossibleValues.Count > 0)
            {
                foreach (string specificValue in fd.PossibleValues)
                {
                    var scenario = new Dictionary<string, string>{{fd.FieldName, specificValue}};
                    sample.Scenario.Add(scenario);
                }
                sample.AllVariations = false;
            }
            else
            {
                // Otherwise, get every value represented in that field
                var scenario = new Dictionary<string, string>{{fd.FieldName, ""}};
                sample.Scenario.Add(scenario);
                sample.AllVariations = true;
            }

            Samples.Add(sample);
        }
    }

    public void GenerateCombinedSamples(IEnumerable<FieldDefinition> fieldDefinitions)
    {
        var sampleScenarioCollection = new Dictionary<string, IDictionary<string, List<string>>>();
        int amount = 2;
        // Iterate through the field definitions and populate the Samples dictionary
        foreach (FieldDefinition fd in fieldDefinitions)
        {
            // Default sample amount per field is 2, or get the value from the samples spreadsheet
            if (Int32.TryParse(fd.DataExample, out int parsedAmount))
            {
                if ( parsedAmount > amount ){ amount = parsedAmount; }
            }

            var scenario = new List<string>();
            // Check if there are specific values required for each field
            if (fd.PossibleValues != null && fd.PossibleValues.Count > 0)
            {
                scenario = fd.PossibleValues;
            }
            else
            {
                // Otherwise, get every value represented in that field
                scenario = new List<string> { "" };
            }

            if (sampleScenarioCollection.TryGetValue(fd.Type, out IDictionary<string, List<string>> scenarios)) {
                scenarios[fd.FieldName] = scenario;
            }
            else {
                sampleScenarioCollection[fd.Type] = new Dictionary<string, List<string>>
                {
                    { fd.FieldName, scenario }
                };
            }
        }

        foreach ( var sampleScenario in sampleScenarioCollection ) {
            var uniqueCombinations = GenerateUniqueCombinations(sampleScenario.Value);
            foreach (var combination in uniqueCombinations) {
                Sample sample = new();
                sample.Scenario.Add(combination);
                sample.Combination = sampleScenario.Key;
                sample.Amount = amount;
                Samples.Add(sample);
            }
        }

    }

    static List<Dictionary<string, string>> GenerateUniqueCombinations(IDictionary<string, List<string>> fields)
    {
        // Start with a single empty combination
        var combinations = new List<Dictionary<string, string>> { new Dictionary<string, string>() };

        foreach (var field in fields)
        {
            var currentField = field.Key;
            var possibleValues = field.Value;

            // Expand combinations for the current field
            combinations = combinations
                .SelectMany(existingCombination =>
                    possibleValues.Select(value =>
                    {
                        var newCombination = new Dictionary<string, string>(existingCombination)
                        {
                            [currentField] = value
                        };
                        return newCombination;
                    }))
                .ToList();
        }

        return combinations;
    }
}
