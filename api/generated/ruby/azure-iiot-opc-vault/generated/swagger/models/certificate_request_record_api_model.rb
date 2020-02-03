# encoding: utf-8
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for
# license information.
#
# Code generated by Microsoft (R) AutoRest Code Generator 1.0.0.0
# Changes may cause incorrect behavior and will be lost if the code is
# regenerated.

module azure.iiot.opc.vault
  module Models
    #
    # Certificate request record model
    #
    class CertificateRequestRecordApiModel
      # @return [String] Request id
      attr_accessor :request_id

      # @return [String] Application id
      attr_accessor :entity_id

      # @return [String] Trust group
      attr_accessor :group_id

      # @return [CertificateRequestState] Possible values include: 'New',
      # 'Approved', 'Rejected', 'Failure', 'Completed', 'Accepted'
      attr_accessor :state

      # @return [CertificateRequestType] Possible values include:
      # 'SigningRequest', 'KeyPairRequest'
      attr_accessor :type

      # @return Error diagnostics
      attr_accessor :error_info

      # @return [VaultOperationContextApiModel]
      attr_accessor :submitted

      # @return [VaultOperationContextApiModel]
      attr_accessor :approved

      # @return [VaultOperationContextApiModel]
      attr_accessor :accepted


      #
      # Mapper for CertificateRequestRecordApiModel class as Ruby Hash.
      # This will be used for serialization/deserialization.
      #
      def self.mapper()
        {
          client_side_validation: true,
          required: false,
          serialized_name: 'CertificateRequestRecordApiModel',
          type: {
            name: 'Composite',
            class_name: 'CertificateRequestRecordApiModel',
            model_properties: {
              request_id: {
                client_side_validation: true,
                required: false,
                serialized_name: 'requestId',
                type: {
                  name: 'String'
                }
              },
              entity_id: {
                client_side_validation: true,
                required: false,
                serialized_name: 'entityId',
                type: {
                  name: 'String'
                }
              },
              group_id: {
                client_side_validation: true,
                required: false,
                serialized_name: 'groupId',
                type: {
                  name: 'String'
                }
              },
              state: {
                client_side_validation: true,
                required: false,
                serialized_name: 'state',
                type: {
                  name: 'Enum',
                  module: 'CertificateRequestState'
                }
              },
              type: {
                client_side_validation: true,
                required: false,
                serialized_name: 'type',
                type: {
                  name: 'Enum',
                  module: 'CertificateRequestType'
                }
              },
              error_info: {
                client_side_validation: true,
                required: false,
                serialized_name: 'errorInfo',
                type: {
                  name: 'Object'
                }
              },
              submitted: {
                client_side_validation: true,
                required: false,
                serialized_name: 'submitted',
                type: {
                  name: 'Composite',
                  class_name: 'VaultOperationContextApiModel'
                }
              },
              approved: {
                client_side_validation: true,
                required: false,
                serialized_name: 'approved',
                type: {
                  name: 'Composite',
                  class_name: 'VaultOperationContextApiModel'
                }
              },
              accepted: {
                client_side_validation: true,
                required: false,
                serialized_name: 'accepted',
                type: {
                  name: 'Composite',
                  class_name: 'VaultOperationContextApiModel'
                }
              }
            }
          }
        }
      end
    end
  end
end