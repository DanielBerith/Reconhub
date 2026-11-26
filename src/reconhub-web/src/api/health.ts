import { useQuery } from '@tanstack/react-query'
import { apiClient } from './client'

export const healthQueryKey = ['health'] as const

export function useHealth() {
  return useQuery({
    queryKey: healthQueryKey,
    queryFn: async ({ signal }) => {
      const { data, response } = await apiClient.GET('/api/health', { signal })
      if (!data) {
        throw new Error(`Health check failed with HTTP ${String(response.status)}`)
      }
      return data
    },
    refetchInterval: 30_000,
  })
}
