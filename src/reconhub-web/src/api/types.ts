import type { components } from './schema.gen'

type Schemas = components['schemas']

export type HealthResponse = Schemas['HealthResponse']
export type HealthStatus = Schemas['HealthStatus']
export type HealthCheckResult = Schemas['HealthCheckResult']
