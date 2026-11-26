import js from '@eslint/js'
import pluginQuery from '@tanstack/eslint-plugin-query'
import prettier from 'eslint-config-prettier'
import { defineConfig, globalIgnores } from 'eslint/config'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import globals from 'globals'
import tseslint from 'typescript-eslint'

const useGeneratedClient = 'Call the backend only through the generated API client in src/api.'

export default defineConfig([
  globalIgnores(['dist', 'coverage', 'src/api/schema.gen.ts']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.strictTypeChecked,
      tseslint.configs.stylisticTypeChecked,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
      pluginQuery.configs['flat/recommended'],
    ],
    languageOptions: {
      globals: globals.browser,
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
  },
  {
    // Rule: the app talks to the backend only through the generated client.
    files: ['src/**/*.{ts,tsx}'],
    ignores: ['src/api/**', 'src/test/**', 'src/**/*.test.{ts,tsx}'],
    rules: {
      'no-restricted-globals': ['error', { name: 'fetch', message: useGeneratedClient }],
      'no-restricted-properties': [
        'error',
        { object: 'window', property: 'fetch', message: useGeneratedClient },
        { object: 'globalThis', property: 'fetch', message: useGeneratedClient },
      ],
      'no-restricted-imports': [
        'error',
        {
          paths: [
            { name: 'openapi-fetch', message: useGeneratedClient },
            { name: 'axios', message: useGeneratedClient },
          ],
        },
      ],
    },
  },
  prettier,
])
