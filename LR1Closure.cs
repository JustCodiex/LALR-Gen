namespace ParserGen;

internal partial class LR1 {

    private readonly List<LR1Kernel> Kernels;

    public void GetClosures() {

        // Add entry to list of kernels
        this.Kernels.Add(new(0, new() { this.Item(this.G.Productions[0], 0) }));

        // Define current kernel index
        int i = 0;

        // While kernels to update
        while (i < this.Kernels.Count) { 
        
            // Get kernel
            LR1Kernel kernel = this.Kernels[i];

            // Update kernel closure
            UpdateKernelClosure(kernel);

            // Add gotos
            if (this.Goto(kernel, this.Kernels)) {
                i = 0;
            } else {
                i++;
            }

        }

    }

    private void UpdateKernelClosure(LR1Kernel kernel) {

        // Define kernel closure index
        int i = 0;

        // While closures to update
        while (i < kernel.Closure.Count) {

            // Get new items after dot
            var newItems = kernel.Closure[i].GetAfterDot(this);

            // Add new items to kernel closure
            for (int j = 0; j < newItems.Count; j++) {
                newItems[j].AddUniqueItemsTo(kernel.Closure);
            }

            // go to next closure
            i++;

        }

    }

    private bool Goto(LR1Kernel current, List<LR1Kernel> kernels) {

        // Have we propogated the lookahead items
        bool propogated = false;

        // Define new kernels keyset
        Dictionary<Symbol, Set<LR1Item>> newKernels = new();

        // Loop over all items in closure
        for (int i = 0; i < current.Closure.Count; i++) {

            // Assign
            var item = current.Closure[i];

            // Get next item
            if (item.GetAfterShift(this) is LR1Item nextItem) {

                // Get symbol
                var sym = item.Rule.Rhs[item.Pos];

                // Add symbol to kernel key-set
                current.Keys.Add(sym);

                // Try get newKernel set
                if (!newKernels.ContainsKey(sym)) {
                    newKernels[sym] = new();
                }

                // Add symbol to next
                nextItem.AddUniqueItemsTo(newKernels[sym]);

            }


        }

        // Loop over keys in current
        for (int i = 0; i < current.Keys.Count; i++) {

            // Grab k
            Symbol s = current.Keys[i];

            var newKernel = new LR1Kernel(kernels.Count, newKernels[s]);
            int j = IndexOf(newKernel, kernels);

            if (j < 0) {
                kernels.Add(newKernel);
                j = newKernel.Index;
            } else {
                for (int k = 0; k < newKernel.Items.Count; k++) {
                    propogated |= newKernel.Items[k].AddUniqueItemsTo(kernels[j].Items);
                }
            }

            current.Gotos[s] = j;

        }

        // Return propogated flag
        return propogated;

    }

    private static int IndexOf(LR1Kernel kernel, List<LR1Kernel> kernels) {
        for (int i = 0; i < kernels.Count; i++) {
            if (kernel.Equals(kernels[i])) {
                return i;
            }
        }
        return -1;
    }

}
