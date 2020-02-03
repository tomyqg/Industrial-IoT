/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 *
 * Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
 * Changes may cause incorrect behavior and will be lost if the code is
 * regenerated.
 */

package com.microsoft.azure.iiot.opc.history.models;

import java.util.List;
import com.fasterxml.jackson.annotation.JsonProperty;

/**
 * An expression element in the filter ast.
 */
public class ContentFilterElementApiModel {
    /**
     * Possible values include: 'Equals', 'IsNull', 'GreaterThan', 'LessThan',
     * 'GreaterThanOrEqual', 'LessThanOrEqual', 'Like', 'Not', 'Between',
     * 'InList', 'And', 'Or', 'Cast', 'InView', 'OfType', 'RelatedTo',
     * 'BitwiseAnd', 'BitwiseOr'.
     */
    @JsonProperty(value = "filterOperator")
    private FilterOperatorType filterOperator;

    /**
     * The operands in the element for the operator.
     */
    @JsonProperty(value = "filterOperands")
    private List<FilterOperandApiModel> filterOperands;

    /**
     * Get possible values include: 'Equals', 'IsNull', 'GreaterThan', 'LessThan', 'GreaterThanOrEqual', 'LessThanOrEqual', 'Like', 'Not', 'Between', 'InList', 'And', 'Or', 'Cast', 'InView', 'OfType', 'RelatedTo', 'BitwiseAnd', 'BitwiseOr'.
     *
     * @return the filterOperator value
     */
    public FilterOperatorType filterOperator() {
        return this.filterOperator;
    }

    /**
     * Set possible values include: 'Equals', 'IsNull', 'GreaterThan', 'LessThan', 'GreaterThanOrEqual', 'LessThanOrEqual', 'Like', 'Not', 'Between', 'InList', 'And', 'Or', 'Cast', 'InView', 'OfType', 'RelatedTo', 'BitwiseAnd', 'BitwiseOr'.
     *
     * @param filterOperator the filterOperator value to set
     * @return the ContentFilterElementApiModel object itself.
     */
    public ContentFilterElementApiModel withFilterOperator(FilterOperatorType filterOperator) {
        this.filterOperator = filterOperator;
        return this;
    }

    /**
     * Get the operands in the element for the operator.
     *
     * @return the filterOperands value
     */
    public List<FilterOperandApiModel> filterOperands() {
        return this.filterOperands;
    }

    /**
     * Set the operands in the element for the operator.
     *
     * @param filterOperands the filterOperands value to set
     * @return the ContentFilterElementApiModel object itself.
     */
    public ContentFilterElementApiModel withFilterOperands(List<FilterOperandApiModel> filterOperands) {
        this.filterOperands = filterOperands;
        return this;
    }

}